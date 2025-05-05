// ResourceServer.cs
// A resource server of WelsonJS Editor (WelsonJS.Launcher)
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private List<IResourceTool> _tools = new List<IResourceTool>();
        private readonly HttpClient _httpClient = new HttpClient();
        private static readonly string _defaultMimeType = "application/octet-stream";
        private static readonly Regex _nodePackageRegex = new Regex(@"^[^/@]+@[^/]+/", RegexOptions.Compiled);
        private static readonly List<string[]> CDN_PREFIXES = new List<string[]> {
            new[] { "ajax/libs/" },
            new[] { "npm/", "gh/", "wp/" },
            new[] { "jquery/" },
            new[] { "polyfill/" },
            new[] { "aspnet/" }
        };
        private enum CDN_TYPES: int
        {
            AjaxLibs = 0,
            JsDeliver = 1,
            Jquery = 2,
            Polyfill = 3,
            AspNet = 4
        };

        public ResourceServer(string prefix, string resourceName)
        {
            _prefix = prefix;
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _resourceName = resourceName;
            _httpClient.Timeout = TimeSpan.FromSeconds(30);

            // Add resource tools
            _tools.Add(new ResourceTools.Completion(this, _httpClient));
            _tools.Add(new ResourceTools.Settings(this, _httpClient));
            _tools.Add(new ResourceTools.DevTools(this, _httpClient));
            _tools.Add(new ResourceTools.DnsQuery(this, _httpClient));
            _tools.Add(new ResourceTools.Tfa(this, _httpClient));
            _tools.Add(new ResourceTools.Whois(this, _httpClient));
        }

        public string GetPrefix()
        {
            return _prefix;
        }

        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _cts = new CancellationTokenSource();
            _listener.Start();

            // Open the web browser
            Program.OpenWebBrowser(_prefix);

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
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private async Task ProcessRequest(HttpListenerContext context)
        {
            string path = context.Request.Url.AbsolutePath.TrimStart('/');

            // Serve from a resource name
            if (String.IsNullOrEmpty(path))
            {
                ServeResource(context, GetResource(_resourceName), "text/html");
                return;
            }

            // Serve the favicon.ico file
            if ("favicon.ico".Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                ServeResource(context, GetResource("favicon"), "image/x-icon");
                return;
            }

            // Serve from a resource tool
            foreach (var tool in _tools)
            {
                if (tool.CanHandle(path))
                {
                    await tool.HandleAsync(context, path);
                    return;
                }
            }

            // Serve from the blob server
            if (await ServeBlob(context, path)) return;

            // Fallback to serve from a resource name
            ServeResource(context, GetResource(_resourceName), "text/html");
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
                    using (var client = new HttpClient())
                    {
                        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                        request.Headers.UserAgent.ParseAdd(context.Request.UserAgent);
                        HttpResponseMessage response = await client.SendAsync(request);

                        if (!response.IsSuccessStatusCode)
                        {
                            Trace.TraceError($"Failed to serve blob. URL: {url}, Status: {response.StatusCode}");
                            return false;
                        }

                        data = await response.Content.ReadAsByteArrayAsync();
                        mimeType = response.Content.Headers.ContentType?.MediaType ?? _defaultMimeType;

                        ServeResource(context, data, mimeType);
                        _ = TrySaveCachedBlob(path, data, mimeType);

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError($"Failed to serve blob. URL: {url}, Exception: {ex.Message}");
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

                        ServeResource(context, data, mimeType);
                        return true;
                    }
                }

                // use CDN sources
                if (await TryServeFromCdn(context, path))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<bool> TryServeFromCdn(HttpListenerContext context, string path)
        {
            bool isNodePackageExpression = _nodePackageRegex.IsMatch(path);
            bool isPrefixMatched(CDN_TYPES type)
            {
                if (CDN_PREFIXES[(int)type].Any(prefix => path.StartsWith(prefix)))
                {
                    return true;
                }

                return false;
            }

            var sources = new (bool isMatch, string configKey, Func<string, string> transform)[]
            {
                (isPrefixMatched(CDN_TYPES.AjaxLibs), "CdnJsPrefix", p => p),
                (isPrefixMatched(CDN_TYPES.AjaxLibs), "GoogleApisPrefix", p => p),
                (isNodePackageExpression, "UnpkgPrefix", p => p),
                (isNodePackageExpression, "SkypackPrefix", p => p),
                (isNodePackageExpression, "EsmShPrefix", p => p),
                (isNodePackageExpression, "EsmRunPrefix", p => p),
                (isPrefixMatched(CDN_TYPES.JsDeliver), "JsDeliverPrefix", p => p),
                (isPrefixMatched(CDN_TYPES.Jquery), "JqueryCdnPrefix", p => p.Substring("jquery/".Length)),
                (isPrefixMatched(CDN_TYPES.Polyfill), "CdnJsPrefix", p => p), // polyfill.js from Cloudflare
                (isPrefixMatched(CDN_TYPES.Polyfill), "PolyfillPrefix", p => p.Substring("polyfill/".Length)), // polyfill.js from Fastly
                (isPrefixMatched(CDN_TYPES.AspNet), "AspNetCdnPrefix", p => p.Substring("aspnet/".Length)),
                (true, "BlobStoragePrefix", p => p) // fallback
            };

            foreach (var (isMatch, configKey, transform) in sources)
            {
                if (isMatch)
                {
                    string prefix = Program.GetAppConfig(configKey);
                    if (await ServeBlob(context, transform(path), prefix))
                    {
                        return true;
                    }
                }
            }

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
                Trace.TraceError($"Cache Read Error: {ex.Message}");
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
                Trace.TraceError($"Error: {ex.Message}");
                return false;
            }
        }

        public void ServeResource(HttpListenerContext context)
        {
            ServeResource(context, "<error>Not Found</error>", "application/xml", 404);
        }

        public void ServeResource(HttpListenerContext context, byte[] data, string mimeType = "text/html", int statusCode = 200)
        {
            string xmlHeader = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";

            if (data == null) {
                data = Encoding.UTF8.GetBytes(xmlHeader + "\r\n<error>Could not find the resource.</error>");
                mimeType = "application/xml";
                statusCode = 404;
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = mimeType;
            context.Response.ContentLength64 = data.Length;
            using (Stream outputStream = context.Response.OutputStream)
            {
                outputStream.Write(data, 0, data.Length);
            }
        }

        public void ServeResource(HttpListenerContext context, string data, string mimeType = "text/html", int statusCode = 200) 
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

            ServeResource(context, Encoding.UTF8.GetBytes(data), mimeType, statusCode);
        }

        private byte[] GetResource(string resourceName)
        {
            // Try to fetch embedded resource.
            byte[] data = GetEmbeddedResource(typeof(Program).Namespace + "." + resourceName);
            if (data != null) return data;

            // Fallback: Try to fetch resource from ResourceManager.
            return GetResourceFromManager(resourceName);
        }

        private byte[] GetEmbeddedResource(string fullResourceName)
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

        private byte[] GetResourceFromManager(string resourceName)
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

        private byte[] ConvertIconToBytes(System.Drawing.Icon icon)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                icon.Save(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
