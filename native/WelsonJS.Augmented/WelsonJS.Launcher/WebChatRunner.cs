// WebChatRunner.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Asks a web chat service (Copilot, ChatGPT, ...) in the launcher's browser through the
    /// DevTools protocol (ChromiumDevToolsPrefix): finds the service's open page, or opens
    /// it (starting the browser when it is not running), puts the message into the message
    /// box, sends it, and waits for the answer. Sign in to the service in that window when
    /// it asks; the page waits for the message box for a few minutes.
    ///
    /// The page opens in an app window of the launcher's browser (msedge --app, no tabs or
    /// address bar, 1000x800, like the popup window the editor used), in the launcher's
    /// profile, so the signed-in session is used. The window may stay behind other windows:
    /// during a turn the page is told it has focus (focus emulation), and the launcher starts
    /// the browser without background throttling, so a covered page keeps answering. It is
    /// not minimized, since a minimized page stops rendering.
    ///
    /// Location access is granted to the service's origins (its hosts) before its page is
    /// used (Browser.grantPermissions), so the page does not stop at a permission prompt.
    /// The grant lasts while the browser runs and is not saved in the profile.
    /// </summary>
    public sealed class WebChatRunner
    {
        private const int OpenWaitMs = 60000;      // wait for an opened window (or the started browser)
        private const int ReadyWaitMs = 180000;    // wait for the message box (page load, sign-in)
        private const int DefaultMaxWaitMs = 300000; // wait for the answer
        private const int PollMs = 1500;
        private const int StablePolls = 4;         // unchanged for ~6 s without a "finished" sign = finished
        private const int Shift = 8, Ctrl = 2;


        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        private readonly ILogger _logger;
        private readonly object _sync = new object();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _turnLocks = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.Ordinal);
        private JsCore _js;
        private Dictionary<string, object> _services;
        // The services granted permissions, with the browser (DevTools URL) they were granted in.
        private readonly ConcurrentDictionary<string, string> _granted = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        private static readonly string[] GrantedPermissions = { "geolocation" };


        public WebChatRunner(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>The services: {key: {name, url, hosts, preferredPath, maxWaitMs}}.</summary>
        public Dictionary<string, object> Services
        {
            get
            {
                lock (_sync)
                {
                    if (_services == null)
                        _services = JsonCodec.DeserializeObject(Js().EvaluateToString("JSON.stringify(WelsonJSWebChat.describe())", "webchat-describe"));
                    return _services;
                }
            }
        }

        public bool HasService(string key)
        {
            return !string.IsNullOrEmpty(key) && Services.ContainsKey(key);
        }

        /// <summary>
        /// The text sent for a conversation: a single user message as it is; otherwise all
        /// messages in one text (a chat service receives one message per turn and does not
        /// know the conversation), system messages as notes at the top.
        /// </summary>
        public static string BuildMessage(object[] messages)
        {
            var list = messages.OfType<Dictionary<string, object>>().ToList();
            if (list.Count == 1 && "user".Equals(Role(list[0])))
                return Content(list[0]);

            var lines = new List<string> { "The conversation so far, with tool results. Reply to the last [user] message." };
            lines.AddRange(list.Where(m => "system".Equals(Role(m))).Select(Content));
            lines.Add("");
            foreach (var message in list.Where(m => !"system".Equals(Role(m))))
            {
                lines.Add("[" + (Role(message) ?? "user") + "]");
                lines.Add(Content(message));
                lines.Add("");
            }
            return string.Join("\n", lines).Trim();
        }

        /// <summary>Sends the text to the service and returns its answer.</summary>
        public async Task<string> ChatAsync(string key, string text, CancellationToken token)
        {
            var service = Services.TryGetValue(key ?? "", out object value) ? value as Dictionary<string, object> : null;
            if (service == null) throw new ArgumentException("Unknown web chat service: " + key);
            string name = Convert.ToString(service["name"]);

            SemaphoreSlim turnLock = _turnLocks.GetOrAdd(key, k => new SemaphoreSlim(1, 1));
            await turnLock.WaitAsync(token);
            try
            {
                Dictionary<string, object> scripts;
                lock (_sync)
                {
                    scripts = JsonCodec.DeserializeObject(Js().EvaluateToString(
                        "JSON.stringify(WelsonJSWebChat.turn(" + JsonCodec.Serialize(key) + ", " + JsonCodec.Serialize(text) + "))", "webchat-turn"));
                }

                await GrantPermissionsAsync(key, service, token);
                string wsUrl = await GetOrOpenPageAsync(key, service, name, token);
                return await RunTurnAsync(service, name, scripts, text, wsUrl, token);
            }
            finally
            {
                turnLock.Release();
            }
        }

        // Puts the message into the chat page, sends it, and waits for a new answer (the
        // same steps the editor took before the launcher did it).
        private async Task<string> RunTurnAsync(Dictionary<string, object> service, string name, Dictionary<string, object> scripts, string message, string wsUrl, CancellationToken token)
        {
            using (DevToolsSession session = await DevToolsSession.ConnectAsync(wsUrl, name, token))
            {
                // The page may be behind other windows: let it behave as focused, for pages
                // that check focus before sending or rendering. Lasts while this session does.
                try { await session.SendAsync("Emulation.setFocusEmulationEnabled", new Dictionary<string, object> { ["enabled"] = true }, token); }
                catch (InvalidOperationException ex) { _logger?.Warn("[WebChat] Focus emulation is not available: " + ex.Message); }

                string input = JsonCodec.Serialize(Convert.ToString(scripts["inputSelector"]));
                string submitSelector = JsonCodec.Serialize(Convert.ToString(scripts["submitSelector"]));
                string stateScript = Convert.ToString(scripts["stateScript"]);
                string prepareScript = scripts["prepareScript"] as string;
                string finishScript = scripts["finishScript"] as string;

                // Focuses the message box; returns its tag ('TEXTAREA', 'DIV', ...) or null.
                string focusInput = "(() => { const el = document.querySelector(" + input + "); if (!el) return null; el.focus(); return el.tagName; })()";
                string inputText = "(() => { const el = document.querySelector(" + input + "); return el ? (el.value !== undefined ? el.value : el.innerText) : ''; })()";
                // Clicks the send button nearest to the message box: inside its form if it has
                // one, otherwise in the closest ancestor that contains one.
                string submit = "(() => {" +
                    " const el = document.querySelector(" + input + ");" +
                    " if (!el) return 'no-input';" +
                    " const form = el.closest('form');" +
                    " for (let node = form || el.parentElement; node; node = form ? null : node.parentElement) {" +
                    "  const button = node.querySelector(" + submitSelector + ");" +
                    "  if (!button) continue;" +
                    "  if (button.disabled || button.getAttribute('aria-disabled') === 'true' || button.matches('[class*=\"--disabled\"], .disabled')) return 'disabled';" +
                    "  button.click();" +
                    "  return 'clicked';" +
                    " }" +
                    " return 'no-button';" +
                    "})()";

                Func<string, Task<object>> eval = expression => session.EvaluateAsync(expression, token);
                Func<Task<bool>> hasText = async () => (Convert.ToString(await eval(inputText)) ?? "").Trim().Length > 0;
                Func<Task> pressEnter = async () =>
                {
                    await eval(focusInput);
                    await session.KeyAsync("Enter", "Enter", 13, 0, "\r", token);
                };
                Func<Task<double>> count = async () => Count(await eval(stateScript));

                // A page that was just opened may still be loading or waiting for sign-in.
                DateTime readyDeadline = DateTime.UtcNow.AddMilliseconds(ReadyWaitMs);
                string tag;
                while ((tag = await eval(focusInput) as string) == null)
                {
                    if (DateTime.UtcNow >= readyDeadline)
                        throw new TimeoutException("Could not find the " + name + " message box. Sign in to " + name + " in its window, then try again.");
                    await Task.Delay(PollMs, token);
                }

                // Service options to set before sending (DeepSeek: turn on DeepThink).
                if (prepareScript != null && true.Equals(await eval(prepareScript)))
                    await Task.Delay(500, token);

                double before = await count();

                // Clear a draft left in the box. Editors such as ProseMirror track the
                // selection themselves, so use real keys.
                Func<Task> clearDraft = async () =>
                {
                    if (await hasText())
                    {
                        await eval(focusInput);
                        await session.KeyAsync("a", "KeyA", 65, Ctrl, null, token);
                        await session.KeyAsync("Backspace", "Backspace", 8, 0, null, token);
                    }
                };
                await clearDraft();

                // Put the text in with the service's method; fall back to typing it.
                string setTextScript = Convert.ToString(scripts["setTextScript"]);
                bool placed = tag != "TEXTAREA" && true.Equals(await eval(setTextScript));
                if (placed)
                {
                    // The editor may apply the change asynchronously.
                    placed = false;
                    for (int i = 0; i < 10 && !(placed = await hasText()); i++)
                        await Task.Delay(100, token);
                }
                if (!placed)
                {
                    await clearDraft();
                    if (tag == "TEXTAREA")
                    {
                        await session.SendAsync("Input.insertText", new Dictionary<string, object> { ["text"] = message }, token);
                    }
                    else
                    {
                        // Line by line with Shift+Enter in a contenteditable box.
                        string[] lines = message.Split('\n');
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (i > 0)
                            {
                                await session.KeyAsync("Enter", "Enter", 13, Shift, "\r", token);
                                await Task.Delay(20, token);
                            }
                            if (lines[i].Length > 0)
                                await session.SendAsync("Input.insertText", new Dictionary<string, object> { ["text"] = lines[i] }, token);
                        }
                    }
                }
                if (!await hasText())
                    throw new InvalidOperationException("The message could not be typed into the " + name + " message box.");

                // Send: click the send button once it is enabled; without one, press Enter.
                string result = "disabled";
                for (int i = 0; i < 10 && (result == "disabled" || (result == "no-button" && i < 3)); i++)
                {
                    await Task.Delay(300, token);
                    result = await eval(submit) as string;
                }
                if (result != "clicked")
                    await pressEnter();

                // Make sure the message left the box or a new turn started.
                bool sent = false;
                for (int i = 0; i < 2 && !sent; i++)
                {
                    if (i > 0) await pressEnter(); // the click (or key) had no effect; try Enter once more
                    for (int w = 0; w < 10 && !(sent = await count() > before || !await hasText()); w++)
                        await Task.Delay(500, token);
                }
                if (!sent)
                    throw new InvalidOperationException("The message was typed but " + name + " did not send it (the send button and Enter had no effect).");

                int maxWaitMs = service.TryGetValue("maxWaitMs", out object max) && max != null ? Convert.ToInt32(max) : DefaultMaxWaitMs;
                DateTime deadline = DateTime.UtcNow.AddMilliseconds(maxWaitMs);
                string last = "";
                int stable = 0;
                while (DateTime.UtcNow < deadline)
                {
                    await Task.Delay(PollMs, token);
                    var state = await eval(stateScript) as Dictionary<string, object>;
                    string answer = state != null && state.TryGetValue("text", out object t) ? t as string : null;
                    if (state == null || Count(state) <= before || string.IsNullOrEmpty(answer))
                    {
                        stable = 0;
                        continue;
                    }
                    stable = answer == last ? stable + 1 : 0;
                    last = answer;
                    // Finished: the page marks the answer as finished and the text has stopped
                    // changing, or the text has not changed for a while.
                    bool done = state.TryGetValue("done", out object d) && true.Equals(d);
                    if ((done && stable >= 1) || stable >= StablePolls)
                    {
                        // Something to do on the page once the answer is taken (Meta AI: choose
                        // the response that was read when it offers two).
                        if (finishScript != null)
                            await eval(finishScript);
                        return last;
                    }
                }
                throw new TimeoutException(name + " did not finish answering within " + (maxWaitMs / 1000) + " seconds." +
                    (last.Length > 0 ? " Partial answer:\n" + last : ""));
            }
        }

        // The DevTools WebSocket URL of the service's page, opening the page when none is open.
        private async Task<string> GetOrOpenPageAsync(string key, Dictionary<string, object> service, string name, CancellationToken token)
        {
            string url = Convert.ToString(service["url"]);
            Dictionary<string, object> page;
            Dictionary<string, object> version = await GetJsonAsync("json/version", token, throwOnError: false) as Dictionary<string, object>;
            if (version == null)
            {
                // The browser is not running (with remote debugging): start it with the page.
                _logger?.Info("[WebChat] Starting the browser for " + name);
                Program.OpenWebBrowser(url, appMode: true);
                page = await WaitForPageAsync(() => FindPageAsync(service, token), name, token);
                // The browser was not running before, so the permissions are granted now.
                await GrantPermissionsAsync(key, service, token);
            }
            else
            {
                page = await FindPageAsync(service, token);
                if (page != null) return Convert.ToString(page["webSocketDebuggerUrl"]);

                // Open it in an app window of the running browser (which takes the command).
                // The new page is the one that was not there before, so that a sign-in page
                // on another host is used as well.
                object[] before = await GetJsonAsync("json", token, throwOnError: false) as object[] ?? new object[0];
                var known = new HashSet<string>(before.OfType<Dictionary<string, object>>().Select(t => t["id"] as string));
                Program.OpenWebBrowser(url, appMode: true);
                page = await WaitForPageAsync(async () =>
                {
                    object[] targets = await GetJsonAsync("json", token, throwOnError: false) as object[];
                    var added = targets?.OfType<Dictionary<string, object>>()
                        .Where(t => "page".Equals(t["type"] as string) && !known.Contains(t["id"] as string)).ToList();
                    return added?.FirstOrDefault(t => IsServicePage(service, t)) ?? added?.FirstOrDefault();
                }, name, token);
            }

            await ResizeWindowAsync(Convert.ToString(page["id"]), token);
            return Convert.ToString(page["webSocketDebuggerUrl"]);
        }

        // Grants GrantedPermissions to the service's origins (https://<host>), once per
        // browser run. Does nothing when the browser is not running.
        private async Task GrantPermissionsAsync(string key, Dictionary<string, object> service, CancellationToken token)
        {
            var version = await GetJsonAsync("json/version", token, throwOnError: false) as Dictionary<string, object>;
            if (version == null) return;
            string browserUrl = Convert.ToString(version["webSocketDebuggerUrl"]);
            if (_granted.TryGetValue(key, out string grantedIn) && grantedIn == browserUrl) return;

            try
            {
                using (DevToolsSession browser = await DevToolsSession.ConnectAsync(browserUrl, "the browser", token))
                {
                    foreach (string host in (service["hosts"] as object[] ?? new object[0]).Select(Convert.ToString))
                    {
                        await browser.SendAsync("Browser.grantPermissions", new Dictionary<string, object>
                        {
                            ["origin"] = "https://" + host,
                            ["permissions"] = GrantedPermissions
                        }, token);
                    }
                }
                _granted[key] = browserUrl;
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                _logger?.Warn("[WebChat] Could not grant permissions for " + key + ": " + ex.Message);
            }
        }

        // Sizes the page's window like the editor's popup window (1000x800).
        private async Task ResizeWindowAsync(string targetId, CancellationToken token)
        {
            try
            {
                var version = await GetJsonAsync("json/version", token, throwOnError: true) as Dictionary<string, object>;
                using (DevToolsSession browser = await DevToolsSession.ConnectAsync(Convert.ToString(version["webSocketDebuggerUrl"]), "the browser", token))
                {
                    Dictionary<string, object> window = await browser.SendAsync("Browser.getWindowForTarget", new Dictionary<string, object> { ["targetId"] = targetId }, token);
                    await browser.SendAsync("Browser.setWindowBounds", new Dictionary<string, object>
                    {
                        ["windowId"] = window["windowId"],
                        ["bounds"] = new Dictionary<string, object> { ["width"] = 1000, ["height"] = 800 }
                    }, token);
                }
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                _logger?.Warn("[WebChat] Could not size the window: " + ex.Message);
            }
        }

        private static async Task<Dictionary<string, object>> WaitForPageAsync(Func<Task<Dictionary<string, object>>> find, string name, CancellationToken token)
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(OpenWaitMs);
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(1000, token);
                Dictionary<string, object> page = await find();
                if (page != null && page.ContainsKey("webSocketDebuggerUrl"))
                    return page;
            }
            throw new TimeoutException("The " + name + " window did not open. Make sure the launcher's browser runs with remote debugging (ChromiumDevToolsPrefix).");
        }

        private static bool IsServicePage(Dictionary<string, object> service, Dictionary<string, object> target)
        {
            var hosts = (service["hosts"] as object[] ?? new object[0]).Select(Convert.ToString);
            return Uri.TryCreate(target["url"] as string, UriKind.Absolute, out Uri uri) && hosts.Contains(uri.Host);
        }

        // Finds an open page of the service, preferring its chat path when it has one.
        private static async Task<Dictionary<string, object>> FindPageAsync(Dictionary<string, object> service, CancellationToken token)
        {
            object[] targets = await GetJsonAsync("json", token, throwOnError: false) as object[];
            if (targets == null) return null;
            var pages = targets.OfType<Dictionary<string, object>>()
                .Where(t => "page".Equals(t["type"] as string) && IsServicePage(service, t)).ToList();

            string preferred = service["preferredPath"] as string;
            if (!string.IsNullOrEmpty(preferred))
            {
                var regex = new Regex(preferred);
                var match = pages.FirstOrDefault(t => regex.IsMatch(new Uri((string)t["url"]).AbsolutePath));
                if (match != null) return match;
            }
            return pages.FirstOrDefault();
        }

        private static async Task<object> GetJsonAsync(string path, CancellationToken token, bool throwOnError)
        {
            string baseUrl = (Program.GetAppConfig("ChromiumDevToolsPrefix") ?? "http://localhost:9222/").TrimEnd('/');
            try
            {
                using (HttpResponseMessage response = await Http.GetAsync(baseUrl + "/" + path, token))
                {
                    response.EnsureSuccessStatusCode();
                    return JsonCodec.Deserialize(await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception) when (!throwOnError && !token.IsCancellationRequested)
            {
                return null;
            }
        }

        // The ChakraCore context with webchat.js loaded (call inside lock (_sync)).
        private JsCore Js()
        {
            if (_js != null) return _js;
            byte[] data = ResourceServer.GetResource("webchat.js");
            if (data == null) throw new FileNotFoundException("webchat.js is not embedded in the launcher.");
            string source = Encoding.UTF8.GetString(data).TrimStart('﻿');
            var js = new JsCore();
            js.EvaluateToString(source + "\n;''", "webchat.js");
            _js = js;
            return _js;
        }

        private static double Count(object state)
        {
            var dict = state as Dictionary<string, object>;
            return dict != null && dict.TryGetValue("count", out object c) && c != null ? Convert.ToDouble(c) : 0;
        }

        private static string Role(Dictionary<string, object> message)
        {
            return message.TryGetValue("role", out object role) ? role as string : null;
        }

        private static string Content(Dictionary<string, object> message)
        {
            return message.TryGetValue("content", out object content) ? Convert.ToString(content) ?? "" : "";
        }

        /// <summary>A minimal DevTools protocol client: SendAsync resolves with the matching result.</summary>
        private sealed class DevToolsSession : IDisposable
        {
            private readonly ClientWebSocket _socket = new ClientWebSocket();
            private readonly ConcurrentDictionary<int, TaskCompletionSource<Dictionary<string, object>>> _pending = new ConcurrentDictionary<int, TaskCompletionSource<Dictionary<string, object>>>();
            private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);
            private readonly CancellationTokenSource _closing = new CancellationTokenSource();
            private int _nextId;

            public static async Task<DevToolsSession> ConnectAsync(string url, string name, CancellationToken token)
            {
                var session = new DevToolsSession();
                try
                {
                    await session._socket.ConnectAsync(new Uri(url), token);
                }
                catch (Exception ex)
                {
                    session.Dispose();
                    throw new InvalidOperationException("Could not connect to " + name + " (DevTools): " + ex.Message, ex);
                }
                _ = session.ReceiveLoopAsync();
                return session;
            }

            public async Task<Dictionary<string, object>> SendAsync(string method, Dictionary<string, object> parameters, CancellationToken token)
            {
                int id = Interlocked.Increment(ref _nextId);
                var completion = new TaskCompletionSource<Dictionary<string, object>>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pending[id] = completion;
                byte[] bytes = Encoding.UTF8.GetBytes(JsonCodec.Serialize(new Dictionary<string, object>
                {
                    ["id"] = id,
                    ["method"] = method,
                    ["params"] = parameters ?? new Dictionary<string, object>()
                }));
                await _sendLock.WaitAsync(token);
                try
                {
                    await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
                }
                finally
                {
                    _sendLock.Release();
                }
                using (token.Register(() => completion.TrySetCanceled()))
                    return await completion.Task;
            }

            public async Task<object> EvaluateAsync(string expression, CancellationToken token)
            {
                Dictionary<string, object> result = await SendAsync("Runtime.evaluate",
                    new Dictionary<string, object> { ["expression"] = expression, ["returnByValue"] = true }, token);
                var value = result != null && result.TryGetValue("result", out object r) ? r as Dictionary<string, object> : null;
                return value != null && value.TryGetValue("value", out object v) ? v : null;
            }

            // text: the character the key produces ("\r" for Enter), so that the page also
            // receives keypress/beforeinput like a real key press.
            public async Task KeyAsync(string key, string code, int keyCode, int modifiers, string text, CancellationToken token)
            {
                var down = new Dictionary<string, object>
                {
                    ["type"] = text != null ? "keyDown" : "rawKeyDown",
                    ["key"] = key,
                    ["code"] = code,
                    ["windowsVirtualKeyCode"] = keyCode,
                    ["modifiers"] = modifiers
                };
                if (text != null)
                {
                    down["text"] = text;
                    down["unmodifiedText"] = text;
                }
                await SendAsync("Input.dispatchKeyEvent", down, token);
                await SendAsync("Input.dispatchKeyEvent", new Dictionary<string, object>
                {
                    ["type"] = "keyUp",
                    ["key"] = key,
                    ["code"] = code,
                    ["windowsVirtualKeyCode"] = keyCode,
                    ["modifiers"] = modifiers
                }, token);
            }

            private async Task ReceiveLoopAsync()
            {
                var buffer = new byte[65536];
                Exception failure = null;
                try
                {
                    while (_socket.State == WebSocketState.Open)
                    {
                        using (var message = new MemoryStream())
                        {
                            WebSocketReceiveResult received;
                            do
                            {
                                received = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), _closing.Token);
                                if (received.MessageType == WebSocketMessageType.Close) return;
                                message.Write(buffer, 0, received.Count);
                            }
                            while (!received.EndOfMessage);

                            Dictionary<string, object> response = JsonCodec.DeserializeObject(Encoding.UTF8.GetString(message.ToArray()));
                            // Events (no id) are ignored.
                            if (response == null || !response.TryGetValue("id", out object id) || id == null) continue;
                            if (!_pending.TryRemove(Convert.ToInt32(id), out var completion)) continue;
                            if (response.TryGetValue("error", out object error) && error != null)
                            {
                                var e = error as Dictionary<string, object>;
                                completion.TrySetException(new InvalidOperationException("DevTools error: " +
                                    (e != null && e.TryGetValue("message", out object m) ? Convert.ToString(m) : JsonCodec.Serialize(error))));
                            }
                            else
                            {
                                completion.TrySetResult(response.TryGetValue("result", out object result) ? result as Dictionary<string, object> : null);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    var closed = new InvalidOperationException("The DevTools connection was closed." + (failure != null && !_closing.IsCancellationRequested ? " " + failure.Message : ""));
                    foreach (int key in _pending.Keys.ToList())
                        if (_pending.TryRemove(key, out var completion)) completion.TrySetException(closed);
                }
            }

            public void Dispose()
            {
                _closing.Cancel();
                try { _socket.Abort(); } catch { }
                _socket.Dispose();
            }
        }
    }
}
