// ResourceServer.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WelsonJS.Launcher
{
    public class ResourceServer
    {
        private readonly HttpListener _listener;
        private CancellationTokenSource _cts;
        private bool _isRunning;
        private string _prefix;
        private string _resourceName;
        private readonly List<IApiEndpoint> _apis = new List<IApiEndpoint>();
        private readonly SemaphoreSlim _requestSlots = new SemaphoreSlim(32, 32);
        private readonly ApiEndpoints.LlamaCpp _llamaCppEndpoint;
        private readonly ApiEndpoints.LlmGuard _llmGuardEndpoint;
        private readonly ApiEndpoints.JsrtInstance _jsrtInstanceEndpoint;
        private readonly ApiEndpoints.Terminal _terminalEndpoint;
        private readonly TerminalSessionManager _terminalSessions = new TerminalSessionManager();
        private readonly object _stateLock = new object();
        private volatile BlobConfig _blobConfig; // replaced by FetchBlobConfig on a background thread
        private readonly ILogger _logger;

        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string _defaultMimeType = "application/octet-stream";
        private static string[] _allowedOrigins;

        static ResourceServer()
        {
            // Set timeout
            int timeout = int.TryParse(Program.GetAppConfig("HttpClientTimeout"), out timeout) ? timeout : 90;
            _httpClient.Timeout = TimeSpan.FromSeconds(timeout);

            // Set allowed origins (CORS policy)
            TryParseAllowedOrigins();
        }

        public ResourceServer(string prefix, string resourceName, ILogger logger = null)
        {
            // Set the logger
            _logger = logger ?? new TraceLogger(typeof(ResourceServer));

            // Initialize
            _prefix = prefix;
            _listener = new HttpListener();
            _resourceName = resourceName;

            // Fetch a blob config from Internet (safe fire-and-forget with logging)
            _ = FetchBlobConfig().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger?.Error($"FetchBlobConfig failed: {t.Exception}");
            }, TaskScheduler.Default);

            // Add API endpoints
            _apis.Add(new ApiEndpoints.Completion(this, _httpClient, _logger));
            _apis.Add(new ApiEndpoints.Settings(this, _httpClient, _logger));
            _apis.Add(new ApiEndpoints.ChromiumDevTools(this, _httpClient, _logger));
            _apis.Add(new ApiEndpoints.DnsQuery(this, _httpClient, _logger));
            _apis.Add(new ApiEndpoints.IpQuery(this, _httpClient, _logger));
            _apis.Add(new ApiEndpoints.Tfa(this, _httpClient, _logger));
            _apis.Add(new ApiEndpoints.Whois(this, _httpClient, _logger));
            _apis.Add(new ApiEndpoints.ImageColorPicker(this, _httpClient, _logger));
            _llamaCppEndpoint = new ApiEndpoints.LlamaCpp(this, _httpClient, _logger);
            _apis.Add(_llamaCppEndpoint);
            _llmGuardEndpoint = new ApiEndpoints.LlmGuard(this, _httpClient, _logger);
            _apis.Add(_llmGuardEndpoint);
            _jsrtInstanceEndpoint = new ApiEndpoints.JsrtInstance(this, _httpClient, _logger);
            _apis.Add(_jsrtInstanceEndpoint);
            _terminalEndpoint = new ApiEndpoints.Terminal(this, _httpClient, _logger);
            _apis.Add(_terminalEndpoint);

            // Register the prefix
            _listener.Prefixes.Add(prefix);
        }

        public TerminalSessionManager TerminalSessions
        {
            get { return _terminalSessions; }
        }

        public string GetPrefix()
        {
            return _prefix;
        }

        public void Start(bool IsOpenWebBrowser = true)
        {
            CancellationToken token;
            lock (_stateLock)
            {
                if (_isRunning) return;

                // Throws HttpListenerException (for example, the port is in use or the
                // URL is not reserved); the server then stays stopped and can be started again.
                _listener.Start();

                _cts = new CancellationTokenSource();
                token = _cts.Token;
                _isRunning = true;
            }

            // Open the web browser
            if (IsOpenWebBrowser)
            {
                Program.OpenWebBrowser(_prefix);
            }

            // Run a task with cancellation token
            Task.Run(() => ListenLoop(token));
        }

        /// <summary>
        /// Stops accepting requests, then stops the processes the endpoints started.
        /// Safe to call more than once, and does not interact with the user.
        /// </summary>
        public void Stop()
        {
            lock (_stateLock)
            {
                if (!_isRunning) return;
                _isRunning = false;

                if (_cts != null) _cts.Cancel();
                // Stop the listener first so that no request can start a new child
                // process while the existing ones are being shut down.
                _listener.Stop();
            }

            ShutdownChildProcesses();
        }

        /// <summary>
        /// Stops the processes the endpoints started (llama.cpp servers, including the
        /// LLM guard, JSRT instances and terminal sessions). Called when the launcher exits.
        /// </summary>
        public void ShutdownChildProcesses()
        {
            _llamaCppEndpoint.Shutdown();
            _llmGuardEndpoint.Shutdown();
            _jsrtInstanceEndpoint.Shutdown();
            _terminalEndpoint.Shutdown();
        }

        public bool IsRunning()
        {
            return _isRunning;
        }

        private async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    // Keep accepting requests while a client is uploading a body or
                    // an endpoint is waiting on an external process.
                    _ = Task.Run(() => DispatchRequest(context, token));
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested || !_isRunning) break;

                    // A stopped or disposed listener fails every call; do not spin.
                    if (!_listener.IsListening)
                    {
                        _logger?.Error($"The listener stopped unexpectedly: {ex.Message}");
                        break;
                    }

                    _logger?.Error($"Error: {ex.Message}");
                }
            }
        }

        private async Task DispatchRequest(HttpListenerContext context, CancellationToken token)
        {
            try
            {
                await _requestSlots.WaitAsync(token);
                try { await ProcessRequest(context); }
                finally { _requestSlots.Release(); }
            }
            catch (OperationCanceledException) { context.Response.Abort(); }
            catch (Exception ex)
            {
                _logger?.Error($"Request failed: {ex.Message}");
                try { context.Response.StatusCode = 500; context.Response.Close(); } catch { }
            }
        }

        private async Task ProcessRequest(HttpListenerContext context)
        {
            // Answer CORS preflight requests for every path, including API endpoints.
            if (HandleCorsPreflight(context))
                return;

            string path = context.Request.Url.AbsolutePath.TrimStart('/');

            // Serve from a resource name
            if (String.IsNullOrEmpty(path))
            {
                await ServeResource(context, GetResource(_resourceName), "text/html");
                return;
            }

            // Serve the favicon.ico file
            if ("favicon.ico".Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                await ServeResource(context, GetResource("favicon"), "image/x-icon");
                return;
            }

            // Serve the pages that the editor opens in separate windows
            foreach (string page in new[] { "terminal.html", "prompt.html" })
            {
                if (page.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    await ServeResource(context, GetResource(page), "text/html");
                    return;
                }
            }

            // Serve via API endpoints. The endpoints run local processes (scripts,
            // terminals, llama.cpp), drive the browser through DevTools, expose settings
            // that include API keys, and spend the user's API quota, so every API request
            // must come from a trusted local caller (see IsTrustedLocalRequest). Endpoints
            // that start processes check this again.
            foreach (var api in _apis)
            {
                if (api.CanHandle(context, path))
                {
                    if (!IsTrustedLocalRequest(context))
                    {
                        _logger?.Warn($"Rejected an untrusted API request: {context.Request.HttpMethod} /{path} from {context.Request.RemoteEndPoint}, Origin: {context.Request.Headers["Origin"] ?? "(none)"}, Sec-Fetch-Site: {context.Request.Headers["Sec-Fetch-Site"] ?? "(none)"}");
                        await ServeResource(context, "{\"error\":\"This API is only available to the launcher and local scripts.\"}", "application/json", 403);
                        return;
                    }

                    await api.HandleAsync(context, path);
                    return;
                }
            }

            // Serve from the blob server (read-only: other methods are never proxied)
            if (IsReadRequest(context) && await ServeBlob(context, path)) return;

            // Fallback to 404 (Not Found)
            await ServeResource(context);
        }

        private async Task<bool> ServeBlob(HttpListenerContext context, string path, string prefix = null)
        {
            byte[] data;
            string mimeType;

            if (!String.IsNullOrEmpty(prefix))
            {
                string url = $"{prefix}{path}";

                try
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        var ua = context?.Request?.UserAgent;
                        if (!string.IsNullOrEmpty(ua))
                        {
                            request.Headers.UserAgent.ParseAdd(ua);
                        }

                        using (HttpResponseMessage response = await _httpClient.SendAsync(request))
                        {
                            if (!response.IsSuccessStatusCode)
                            {
                                _logger?.Error($"Failed to serve blob. URL: {url}, Status: {response.StatusCode}");
                                return false;
                            }

                            data = await response.Content.ReadAsByteArrayAsync();
                            mimeType = response.Content.Headers.ContentType?.MediaType ?? _defaultMimeType;
                        }
                    }

                    await ServeBlobResource(context, data, mimeType);
                    _ = TrySaveCachedBlob(path, data, mimeType);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.Error($"Failed to serve blob. URL: {url}, Exception: {ex.Message}");
                    return false;
                }
            }
            else
            {
                // use the cached data
                if (TryGetCachedBlob(path, out mimeType, true))
                {
                    if (TryGetCachedBlob(path, out data))
                    {
                        if (String.IsNullOrEmpty(mimeType))
                        {
                            mimeType = _defaultMimeType;
                        }

                        await ServeBlobResource(context, data, mimeType);
                        return true;
                    }
                }

                // use a blob source
                if (await TryServeFromBlob(context, path))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> TryServeFromBlob(HttpListenerContext context, string path)
        {
            if (_blobConfig != null)
            {
                foreach (var route in _blobConfig.Routes)
                {
                    foreach (var (regex, index) in route.RegexConditions.Select((r, i) => (r, i)))
                    {
                        if (!regex.Compiled.IsMatch(path)) continue;

                        var match = (index < route.Matches.Count) ? route.Matches[index] : route.Matches.First();
                        var _path = route.StripPrefix ? path.Substring(match.Length) : path;

                        foreach (var prefixUrl in route.PrefixUrls)
                        {
                            if (await ServeBlob(context, _path, prefixUrl))
                                return true;
                        }
                    }
                }
            }

            // fallback
            string prefix = Program.GetAppConfig("BlobStoragePrefix");
            if (await ServeBlob(context, path, prefix))
                return true;

            return false;
        }

        private string GetCachedBlobPath(string path)
        {
            // Get a hash from the path
            string hashedPath;
            using (MD5 md5 = MD5.Create())
            {
                byte[] bHashedPath = md5.ComputeHash(Encoding.UTF8.GetBytes(path));
                hashedPath = BitConverter.ToString(bHashedPath).Replace("-", "").ToLowerInvariant();
            }

            // Get a sub-directory paths from the hashed path
            string[] subDirectoryPaths = new string[] {
                hashedPath.Substring(0, 2),
                hashedPath.Substring(2, 2),
                hashedPath.Substring(4, 2)
            };

            // Return the cache path
            return Path.Combine(Program.GetAppDataPath(), "BlobCache", String.Join("\\", subDirectoryPaths), hashedPath);
        }

        private bool TryGetCachedBlob(string path, out byte[] data, bool isMetadata = false)
        {
            string cachePath = GetCachedBlobPath(path);
            if (isMetadata)
            {
                cachePath = $"{cachePath}.meta";
            }

            try
            {
                if (File.Exists(cachePath))
                {
                    data = File.ReadAllBytes(cachePath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Cache Read Error: {ex.Message}");
            }

            data = null;
            return false;
        }

        private bool TryGetCachedBlob(string path, out string data, bool isMetadata = false)
        {
            byte[] bData;
            if (TryGetCachedBlob(path, out bData, isMetadata))
            {
                data = Encoding.UTF8.GetString(bData);
                return true;
            }

            data = null;
            return false;
        }

        private async Task<bool> TrySaveCachedBlob(string path, byte[] data, string mimeType)
        {
            await Task.Delay(0);

            try
            {
                string cachePath = GetCachedBlobPath(path);
                string cacheDirectory = Path.GetDirectoryName(cachePath);

                // Is exists the cached blob directory
                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                }

                // Write each file completely before it becomes visible, so a concurrent
                // request or an interrupted write never leaves a truncated cache entry.
                // The metadata is written last because a cache hit requires it.
                WriteFileAtomically(cachePath, data);
                WriteFileAtomically($"{cachePath}.meta", Encoding.UTF8.GetBytes(mimeType));

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error: {ex.Message}");
                return false;
            }
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool MoveFileEx(string existingFileName, string newFileName, int flags);

        private const int MOVEFILE_REPLACE_EXISTING = 0x1;
        private const int MOVEFILE_WRITE_THROUGH = 0x8;

        // Writes a temporary file and renames it over the target. Unlike File.Replace,
        // the rename leaves no backup (~RF*.TMP) files behind when it fails.
        private static void WriteFileAtomically(string path, byte[] data)
        {
            string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(tempPath, data);
                if (!MoveFileEx(tempPath, path, MOVEFILE_REPLACE_EXISTING | MOVEFILE_WRITE_THROUGH))
                {
                    int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    // Another request is storing or reading the same entry; keep its copy.
                    if (!File.Exists(path))
                        throw new IOException("Could not store the cache file " + path, new System.ComponentModel.Win32Exception(error));
                }
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                        File.Delete(tempPath);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        public async Task ServeResource(HttpListenerContext context)
        {
            await ServeResource(context, "<error>Not Found</error>", "application/xml", 404);
        }

        /// <summary>
        /// Writes a complete response and closes it. Nothing may be written to the
        /// response afterwards.
        /// </summary>
        public async Task ServeResource(HttpListenerContext context, byte[] data, string mimeType = "text/html", int statusCode = 200)
        {
            TryApplyCors(context);

            string xmlHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";

            if (data == null)
            {
                data = Encoding.UTF8.GetBytes(xmlHeader + "\r\n<error>Could not find the resource.</error>");
                mimeType = "application/xml";
                statusCode = 404;
            }

            try
            {
                // The launcher's pages (editor, terminal, prompt) must not be framed by
                // other sites (clickjacking).
                context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = mimeType;
                context.Response.ContentLength64 = data.Length;

                // A HEAD response carries the headers only.
                if (!string.Equals(context.Request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
                    await context.Response.OutputStream.WriteAsync(data, 0, data.Length);
            }
            finally
            {
                // Completes the response and releases the connection; otherwise it is
                // held until the context is garbage collected.
                context.Response.Close();
            }
        }

        // Content proxied from a blob server is not part of the launcher. HTML and SVG
        // documents are sandboxed into an opaque origin so that, if a blob server or the
        // downloaded blob config is compromised, such a page cannot pass
        // IsTrustedLocalRequest and drive the endpoints that start local processes.
        // Scripts and stylesheets used by the editor are not documents and are unaffected.
        private Task ServeBlobResource(HttpListenerContext context, byte[] data, string mimeType)
        {
            string type = (mimeType ?? "").Split(';')[0].Trim().ToLowerInvariant();
            if (type == "text/html" || type == "application/xhtml+xml" || type == "image/svg+xml")
            {
                context.Response.Headers["Content-Security-Policy"] = "sandbox allow-scripts allow-forms allow-popups";
                context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            }

            return ServeResource(context, data, mimeType);
        }

        private static bool IsReadRequest(HttpListenerContext context)
        {
            string method = context.Request.HttpMethod;
            return string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(method, "HEAD", StringComparison.OrdinalIgnoreCase);
        }

        public async Task ServeResource(HttpListenerContext context, string data, string mimeType = "text/html", int statusCode = 200)
        {
            string xmlHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";

            if (data == null)
            {
                data = xmlHeader + "\r\n<error>Could not find the resource.</error>";
                mimeType = "application/xml";
                statusCode = 404;
            }
            else if (mimeType == "application/xml" && !data.StartsWith("<?xml"))
            {
                data = xmlHeader + "\r\n" + data;
            }

            await ServeResource(context, Encoding.UTF8.GetBytes(data), mimeType, statusCode);
        }

        public static byte[] GetResource(string resourceName)
        {
            // Try to fetch embedded resource.
            byte[] data = GetEmbeddedResource(typeof(Program).Namespace + "." + resourceName);
            if (data != null) return data;

            // Fallback: Try to fetch resource from ResourceManager.
            return GetResourceFromManager(resourceName);
        }

        private static byte[] GetEmbeddedResource(string fullResourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
            {
                if (stream != null)
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        stream.CopyTo(memoryStream);
                        return memoryStream.ToArray();
                    }
                }
            }
            return null;
        }

        private static byte[] GetResourceFromManager(string resourceName)
        {
            object resourceObject = Properties.Resources.ResourceManager.GetObject(resourceName);
            switch (resourceObject)
            {
                case byte[] resourceBytes:
                    return resourceBytes;
                case System.Drawing.Icon icon:
                    return ConvertIconToBytes(icon);
                default:
                    return null;
            }
        }

        private static byte[] ConvertIconToBytes(System.Drawing.Icon icon)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                icon.Save(memoryStream);
                return memoryStream.ToArray();
            }
        }

        private async Task FetchBlobConfig()
        {
            try
            {
                string url = Program.GetAppConfig("BlobConfigUrl");
                if (string.IsNullOrWhiteSpace(url))
                {
                    _logger?.Warn("BlobConfigUrl is not configured.");
                    return;
                }

                using (var response = await _httpClient.GetStreamAsync(url).ConfigureAwait(false))
                using (var reader = new StreamReader(response))
                {
                    var serializer = new XmlSerializer(typeof(BlobConfig));
                    var cfg = (BlobConfig)serializer.Deserialize(reader);
                    cfg?.Compile();
                    _blobConfig = cfg;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"Failed to fetch a blob config. Exception: {ex}");
            }
        }

        private static void TryParseAllowedOrigins(ILogger logger = null)
        {
            var raw = Program.GetAppConfig("ResourceServerAllowOrigins");

            if (!string.IsNullOrEmpty(raw))
            {
                _allowedOrigins = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim())
                                     .Where(s => !string.IsNullOrEmpty(s))
                                     .ToArray();
                return;
            }

            var prefix = Program.GetAppConfig("ResourceServerPrefix");
            if (!string.IsNullOrEmpty(prefix))
            {
                try
                {
                    var uri = new Uri(prefix);
                    _allowedOrigins = new[] { uri.GetLeftPart(UriPartial.Authority) }; // protocol + host + port
                    return;
                }
                catch (Exception ex)
                {
                    logger?.Warn($"Invalid ResourceServerPrefix '{prefix}'. It must be a valid absolute URI. Error: {ex.Message}");
                    // fall through to set empty
                }
            }

            _allowedOrigins = Array.Empty<string>();
        }

        /// <summary>
        /// Returns true when a request may use the API endpoints (which can run local
        /// processes):
        /// <list type="bullet">
        /// <item>the client connects from this machine (loopback remote address), so a
        /// network client cannot pass by sending "Host: localhost" when the listener
        /// prefix is opened to the network;</item>
        /// <item>it targets a loopback host, which also defeats DNS rebinding;</item>
        /// <item>a browser-supplied Origin is this server's own origin or an explicitly
        /// allowed one (a wildcard CORS policy never qualifies, and "null" never matches);</item>
        /// <item>a browser request without an Origin (for example &lt;img&gt; or &lt;script&gt;
        /// GET requests from another site) is rejected when Sec-Fetch-Site reports another
        /// site.</item>
        /// </list>
        /// Non-browser clients such as WSH scripts (MSXML) send neither header.
        /// </summary>
        public static bool IsTrustedLocalRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context?.Request;
            Uri url = request?.Url;
            if (url == null || !url.IsLoopback)
                return false;

            IPEndPoint remote = request.RemoteEndPoint;
            if (remote == null || !IPAddress.IsLoopback(remote.Address))
                return false;

            string origin = request.Headers["Origin"];
            if (!string.IsNullOrEmpty(origin))
            {
                if (string.Equals(origin, url.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
                    return true;

                return (_allowedOrigins ?? Array.Empty<string>()).Contains(origin, StringComparer.Ordinal);
            }

            // No Origin: a non-browser client, a same-origin GET, or a user navigation
            // (Sec-Fetch-Site: same-origin or none). Requests initiated by another site,
            // including other ports on localhost ("same-site"), are not trusted.
            string fetchSite = request.Headers["Sec-Fetch-Site"];
            if (!string.IsNullOrEmpty(fetchSite) &&
                !string.Equals(fetchSite, "same-origin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(fetchSite, "none", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private static bool TryApplyCors(HttpListenerContext context)
        {
            var origin = context.Request.Headers["Origin"];
            if (string.IsNullOrEmpty(origin))
                return false;

            var allowed = _allowedOrigins;
            if (allowed.Length == 0)
                return false;

            var respHeaders = context.Response.Headers;

            if (allowed.Any(a => a == "*"))
            {
                respHeaders["Access-Control-Allow-Origin"] = "*";
                respHeaders["Vary"] = "Origin";
                return true;
            }

            // only perform a single, case-sensitive origin check
            if (allowed.Contains(origin, StringComparer.Ordinal))
            {
                respHeaders["Access-Control-Allow-Origin"] = origin;
                respHeaders["Access-Control-Allow-Credentials"] = "true";
                respHeaders["Vary"] = "Origin";
                return true;
            }

            return false;
        }

        private static bool HandleCorsPreflight(HttpListenerContext context)
        {
            if (!string.Equals(context.Request.HttpMethod, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrEmpty(context.Request.Headers["Origin"]))
                return false;

            // Apply CORS headers once here
            TryApplyCors(context);

            var respHeaders = context.Response.Headers;
            respHeaders["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
            respHeaders["Access-Control-Allow-Headers"] = "Content-Type, Authorization, X-Requested-With";
            respHeaders["Access-Control-Max-Age"] = "600";

            context.Response.StatusCode = 204;
            context.Response.ContentLength64 = 0;
            context.Response.Close();

            return true;
        }
    }

    [XmlRoot("blobConfig")]
    public class BlobConfig
    {
        [XmlArray("routes")]
        [XmlArrayItem("route")]
        public List<BlobRoute> Routes { get; set; } = new List<BlobRoute>();

        public void Compile()
        {
            foreach (var route in Routes)
            {
                if (route.Matches == null) continue;

                route.RegexConditions = new List<RegexCondition>();
                foreach (var match in route.Matches)
                {
                    route.RegexConditions.Add(new RegexCondition
                    {
                        Pattern = match,
                        Compiled = new Regex(
                            match.StartsWith("^") ? match : "^" + Regex.Escape(match),
                            RegexOptions.Compiled)
                    });
                }
            }
        }
    }

    public class BlobRoute
    {
        [XmlArray("matches")]
        [XmlArrayItem("match")]
        public List<string> Matches { get; set; }

        [XmlArray("prefixUrls")]
        [XmlArrayItem("url")]
        public List<string> PrefixUrls { get; set; }

        [XmlAttribute("stripPrefix")]
        public bool StripPrefix { get; set; }

        [XmlIgnore]
        public List<RegexCondition> RegexConditions { get; set; }
    }

    public class RegexCondition
    {
        [XmlIgnore]
        public string Pattern { get; set; }

        [XmlIgnore]
        public Regex Compiled { get; set; }
    }
}
