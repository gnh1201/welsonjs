// Terminal.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace WelsonJS.Launcher.ApiEndpoints
{
    /// <summary>
    /// Interactive terminal sessions backed by ConPTY.
    ///
    ///   GET    terminal/profiles              available shell profiles
    ///   GET    terminal/sessions              list sessions
    ///   POST   terminal/sessions              {profile, cols, rows, cwd, script, instanceId} -> session;
    ///                                         without cwd the given instance is shared, or a
    ///                                         new one is deployed and script (editor content)
    ///                                         becomes its bootstrap.js
    ///   DELETE terminal/sessions/{id}         kill and forget a session
    ///   GET    terminal/sessions/{id}/socket  WebSocket: binary frames carry UTF-8 VT output;
    ///                                         text frames from the client are JSON
    ///                                         {"type":"input","data":"..."} or
    ///                                         {"type":"resize","cols":n,"rows":n};
    ///                                         the server sends {"type":"exit","exitCode":n}.
    ///
    /// A session is a full shell with the launcher's privileges, so every request must
    /// come from a loopback host and, when the browser sends an Origin, from this server's
    /// own origin (see <see cref="ResourceServer.IsTrustedLocalRequest"/>).
    /// </summary>
    public sealed class Terminal : IApiEndpoint
    {
        private const string Prefix = "terminal/";
        private const string SessionsPrefix = Prefix + "sessions/";
        private const long MaxRequestBodyBytes = 1024 * 1024;
        private const int MaxSocketMessageBytes = 1024 * 1024;
        private const int MaxOutputFrameBytes = 64 * 1024;

        private readonly TerminalSessionManager _sessions;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private readonly ILogger _logger;

        public Terminal(ResourceServer server, HttpClient httpClient, ILogger logger)
        {
            _sessions = server.TerminalSessions;
            _logger = logger;
        }

        public bool CanHandle(HttpListenerContext context, string path)
        {
            return context != null && path != null && path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            string method = context.Request.HttpMethod.ToUpperInvariant();
            try
            {
                if (!ResourceServer.IsTrustedLocalRequest(context))
                {
                    await Write(context, new { error = "Terminal access is only allowed from the launcher's own origin on localhost." }, 403);
                    return;
                }

                string rest = path.Substring(Prefix.Length).TrimEnd('/');

                if (rest.Equals("profiles", StringComparison.OrdinalIgnoreCase) && method == "GET")
                {
                    await Write(context, new { profiles = _sessions.Profiles() });
                    return;
                }

                if (rest.Equals("sessions", StringComparison.OrdinalIgnoreCase))
                {
                    if (method == "GET")
                    {
                        await Write(context, new { sessions = _sessions.List() });
                        return;
                    }
                    if (method == "POST")
                    {
                        var input = _serializer.DeserializeObject(await ReadBody(context)) as Dictionary<string, object> ?? new Dictionary<string, object>();
                        var session = _sessions.Create(
                            GetString(input, "profile"),
                            GetShort(input, "cols"),
                            GetShort(input, "rows"),
                            GetString(input, "cwd"),
                            GetString(input, "script"),
                            GetString(input, "instanceId"));
                        _logger.Info($"[Terminal] Started {session.Profile} session {session.Id} (PID: {session.ProcessId}) in {session.WorkingDirectory}");
                        await Write(context, session.ToDictionary());
                        return;
                    }
                }

                if (path.StartsWith(SessionsPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string[] parts = path.Substring(SessionsPrefix.Length).TrimEnd('/').Split('/');
                    TerminalSession session = _sessions.Get(parts[0]);
                    if (session == null)
                    {
                        await Write(context, new { error = "Terminal session not found." }, 404);
                        return;
                    }

                    if (parts.Length == 1 && method == "GET")
                    {
                        await Write(context, session.ToDictionary());
                        return;
                    }
                    if (parts.Length == 1 && method == "DELETE")
                    {
                        _sessions.Close(session.Id);
                        await Write(context, new { closed = true });
                        return;
                    }
                    if (parts.Length == 2 && parts[1].Equals("socket", StringComparison.OrdinalIgnoreCase) && method == "GET")
                    {
                        if (!context.Request.IsWebSocketRequest)
                        {
                            await Write(context, new { error = "WebSocket upgrade required." }, 400);
                            return;
                        }

                        HttpListenerWebSocketContext socketContext = await context.AcceptWebSocketAsync(null);
                        // Run the socket outside the request slot so long-lived terminals
                        // do not starve the server's request concurrency limit.
                        _ = Task.Run(() => RunSocket(socketContext.WebSocket, session));
                        return;
                    }
                }

                await Write(context, new { error = "Not found." }, 404);
            }
            catch (Exception ex)
            {
                _logger.Error("[Terminal] Request failed", ex);
                try { await Write(context, new { error = ex.Message }, 400); } catch { }
            }
        }

        public void Shutdown()
        {
            _sessions.Shutdown();
        }

        private async Task RunSocket(WebSocket socket, TerminalSession session)
        {
            var frames = new ConcurrentQueue<OutgoingFrame>();
            var signal = new SemaphoreSlim(0);
            var cts = new CancellationTokenSource();

            Action<OutgoingFrame> enqueue = frame => { frames.Enqueue(frame); signal.Release(); };
            enqueue(OutgoingFrame.Json(_serializer.Serialize(new Dictionary<string, object>
            {
                ["type"] = "session",
                ["session"] = session.ToDictionary()
            })));

            IDisposable subscription = session.Subscribe(
                chunk => enqueue(OutgoingFrame.Output(chunk)),
                exitCode => enqueue(OutgoingFrame.Json(_serializer.Serialize(new Dictionary<string, object>
                {
                    ["type"] = "exit",
                    ["exitCode"] = exitCode
                }), true)));

            Task sender = SendLoop(socket, frames, signal, cts.Token);
            try
            {
                await ReceiveLoop(socket, session, cts.Token);
            }
            catch (Exception ex) when (ex is WebSocketException || ex is OperationCanceledException || ex is HttpListenerException)
            {
                // Browser tab closed or connection dropped; the session keeps running.
                _logger.Debug($"[Terminal] Socket for session {session.Id} closed: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.Error("[Terminal] Socket failed", ex);
            }
            finally
            {
                subscription.Dispose();
                cts.Cancel();
                try { await sender; } catch { }
                socket.Dispose();
                cts.Dispose();
            }
        }

        private async Task ReceiveLoop(WebSocket socket, TerminalSession session, CancellationToken token)
        {
            var buffer = new byte[8192];
            var message = new MemoryStream();

            while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                if (result.MessageType == WebSocketMessageType.Close) break;

                message.Write(buffer, 0, result.Count);
                if (message.Length > MaxSocketMessageBytes)
                    throw new InvalidDataException("Terminal input message is too large.");
                if (!result.EndOfMessage) continue;

                string text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
                message.SetLength(0);

                if (result.MessageType == WebSocketMessageType.Text)
                    HandleClientMessage(session, text);
            }
        }

        private void HandleClientMessage(TerminalSession session, string text)
        {
            var input = _serializer.DeserializeObject(text) as Dictionary<string, object>;
            if (input == null) return;

            switch (GetString(input, "type"))
            {
                case "input":
                    session.Write(GetString(input, "data"));
                    break;
                case "resize":
                    session.Resize(GetShort(input, "cols"), GetShort(input, "rows"));
                    break;
            }
        }

        private static async Task SendLoop(WebSocket socket, ConcurrentQueue<OutgoingFrame> frames, SemaphoreSlim signal, CancellationToken token)
        {
            var output = new MemoryStream();
            while (!token.IsCancellationRequested)
            {
                await signal.WaitAsync(token);

                OutgoingFrame frame;
                while (frames.TryDequeue(out frame))
                {
                    if (frame.Text == null)
                    {
                        // Coalesce bursts of small output chunks into fewer frames.
                        output.Write(frame.Data, 0, frame.Data.Length);
                        if (output.Length < MaxOutputFrameBytes) continue;
                    }

                    await FlushOutput(socket, output, token);

                    if (frame.Text != null)
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(frame.Text);
                        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
                        if (frame.IsFinal)
                        {
                            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Process exited", token);
                            return;
                        }
                    }
                }
                await FlushOutput(socket, output, token);
            }
        }

        private static async Task FlushOutput(WebSocket socket, MemoryStream output, CancellationToken token)
        {
            if (output.Length == 0) return;
            await socket.SendAsync(new ArraySegment<byte>(output.GetBuffer(), 0, (int)output.Length), WebSocketMessageType.Binary, true, token);
            output.SetLength(0);
        }

        private sealed class OutgoingFrame
        {
            public byte[] Data;
            public string Text;
            public bool IsFinal;

            public static OutgoingFrame Output(byte[] data) { return new OutgoingFrame { Data = data }; }
            public static OutgoingFrame Json(string text, bool isFinal = false) { return new OutgoingFrame { Text = text, IsFinal = isFinal }; }
        }

        private static string GetString(Dictionary<string, object> input, string key)
        {
            object value;
            return input.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : null;
        }

        private static short GetShort(Dictionary<string, object> input, string key)
        {
            object value;
            if (!input.TryGetValue(key, out value) || value == null) return 0;
            try { return (short)Math.Max(0, Math.Min(1000, Convert.ToInt32(value))); }
            catch (FormatException) { return 0; }
        }

        private static async Task<string> ReadBody(HttpListenerContext context)
        {
            if (context.Request.ContentLength64 > MaxRequestBodyBytes) throw new InvalidDataException("Request body is too large.");
            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                string body = await reader.ReadToEndAsync();
                if (body.Length > MaxRequestBodyBytes) throw new InvalidDataException("Request body is too large.");
                return string.IsNullOrWhiteSpace(body) ? "{}" : body;
            }
        }

        private async Task Write(HttpListenerContext context, object value, int status = 200)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(_serializer.Serialize(value));
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            context.Response.Close();
        }
    }
}
