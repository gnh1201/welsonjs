// ResourceServer.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using log4net;
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
using System.Windows.Forms;
using System.Xml.Serialization;

namespace WelsonJS.Launcher
{
    public class ResourceServer
    {
        private readonly HttpListener _listener;
        private CancellationTokenSource _cts;
        private Task _serverTask;
        private bool _isRunning;
        private string _prefix;
        private string _resourceName;
        private readonly List<IApiEndpoint> _apis = new List<IApiEndpoint>();
        private BlobConfig _blobConfig;
        private readonly ILog _logger;

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

        public ResourceServer(string prefix, string resourceName, ILog logger = null)
        {
            // Set the logger
            _logger = logger ?? LogManager.GetLogger(typeof(Program));

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
            _apis.Add(new ApiEndpoints.TwoFactorAuth(this, _httpClient, _logger));
            _apis.Add(new ApiEndpoints.Whois(this, _httpClient, _logger));
            _apis.Add(new ApiEndpoints.ImageColorPicker(this, _httpClient, _logger));
            _apis.Add(new ApiEndpoints.JsonRpc2(this, _httpClient, _logger));

            // Register the prefix
            _listener.Prefixes.Add(prefix);
        }

        public string GetPrefix()
        {
            return _prefix;
        }

        public void Start(bool IsOpenWebBrowser = true)
        {
            if (_isRunning) return;

            _isRunning = true;
            _cts = new CancellationTokenSource();
            _listener.Start();

            // Open the web browser
            if (IsOpenWebBrowser)
            {
                Program.OpenWebBrowser(_prefix);
            }

            // Run a task with cancellation token
            _serverTask = Task.Run(() => ListenLoop(_cts.Token));
        }

        public void Stop()
        {
            _isRunning = false;
            _cts.Cancel();
            _listener.Stop();

            MessageBox.Show("Server stopped.");
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
                    await ProcessRequest(await _listener.GetContextAsync());
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested || !_isRunning) break;
                    _logger?.Error($"Error: {ex.Message}");
                }
            }
        }

        private async Task ProcessRequest(HttpListenerContext context)
        {
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

            // Serve via API endpoints
            foreach (var api in _apis)
            {
                if (api.CanHandle(context, path))
                {
                    await api.HandleAsync(context, path);
                    return;
                }
            }

            // Serve from the blob server
            if (await ServeBlob(context, path)) return;

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
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                    var ua = context?.Request?.UserAgent;
                    if (!string.IsNullOrEmpty(ua))
                    {
                        request.Headers.UserAgent.ParseAdd(ua);
                    }
                    HttpResponseMessage response = await _httpClient.SendAsync(request);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger?.Error($"Failed to serve blob. URL: {url}, Status: {response.StatusCode}");
                        return false;
                    }

                    data = await response.Content.ReadAsByteArrayAsync();
                    mimeType = response.Content.Headers.ContentType?.MediaType ?? _defaultMimeType;

                    await ServeResource(context, data, mimeType);
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

                        await ServeResource(context, data, mimeType);
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

                // Save the cache
                File.WriteAllBytes(cachePath, data);

                // Save the cache meta
                File.WriteAllBytes($"{cachePath}.meta", Encoding.UTF8.GetBytes(mimeType));

                return true;
            }
            catch (Exception ex)
            {
                _logger?.Error($"Error: {ex.Message}");
                return false;
            }
        }

        public async Task ServeResource(HttpListenerContext context)
        {
            await ServeResource(context, "<error>Not Found</error>", "application/xml", 404);
        }

        public async Task ServeResource(HttpListenerContext context, byte[] data, string mimeType = "text/html", int statusCode = 200)
        {
            if (HandleCorsPreflight(context))
                return;

            TryApplyCors(context);

            string xmlHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";

            if (data == null)
            {
                data = Encoding.UTF8.GetBytes(xmlHeader + "\r\n<error>Could not find the resource.</error>");
                mimeType = "application/xml";
                statusCode = 404;
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = mimeType;
            context.Response.ContentLength64 = data.Length;
            await context.Response.OutputStream.WriteAsync(data, 0, data.Length);
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

        private static void TryParseAllowedOrigins(ILog logger = null)
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