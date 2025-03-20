using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace WelsonJS.Launcher.Tools
{
    public class ResourceServer
    {
        private readonly HttpListener _listener;
        private CancellationTokenSource _cts;
        private Task _serverTask;
        private bool _isRunning;
        private string _prefix;
        private string _resourceName;
        private ExecutablesCollector _executablesCollector;

        public ResourceServer(string prefix, string resourceName)
        {
            _prefix = prefix;
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _resourceName = resourceName;
            _executablesCollector = new ExecutablesCollector();
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

            // Serve the favicon.ico file
            if ("favicon.ico".Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                ServeResource(context, GetResource("favicon"), "image/x-icon");
                return;
            }

            // Serve the code completion (word suggestion)
            const string completionPrefix = "completion/";
            if (path.StartsWith(completionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                ServeCompletion(context, path.Substring(completionPrefix.Length));
                return;
            }

            // Serve the DevTools Protocol
            const string devtoolsPrefix = "devtools/";
            if (path.StartsWith(devtoolsPrefix, StringComparison.OrdinalIgnoreCase))
            {
                await ServeDevTools(context, path.Substring(devtoolsPrefix.Length - 1));
                return;
            }

            // Serve WHOIS request (use KRNIC server)
            const string whoisPrefix = "whois/";
            if (path.StartsWith(whoisPrefix, StringComparison.OrdinalIgnoreCase))
            {
                await ServeWhoisRequest(context, path.Substring(whoisPrefix.Length));
                return;
            }

            // Serve DNS Query request (use Google DNS server)
            const string dnsQueryPrefix = "dns-query/";
            if (path.StartsWith(dnsQueryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                ServeDnsQueryRequest(context, path.Substring(dnsQueryPrefix.Length));
                return;
            }

            // Serve a resource
            ServeResource(context, GetResource(_resourceName), "text/html");
        }

        private void ServeCompletion(HttpListenerContext context, string word)
        {
            try
            {
                List<string> executables = _executablesCollector.GetExecutables();

                CompletionItem[] completionItems = executables
                    .Where(exec => exec.IndexOf(word, 0, StringComparison.OrdinalIgnoreCase) > -1)
                    .Take(100) // Limit the number of results
                    .Select(exec => new CompletionItem
                    {
                        Label = Path.GetFileName(exec),
                        Kind = "Text",
                        Documentation = $"An executable file: {exec}",
                        InsertText = exec
                    })
                    .ToArray();

                XElement response = new XElement("suggestions",
                    completionItems.Select(item => new XElement("item",
                        new XElement("label", item.Label),
                        new XElement("kind", item.Kind),
                        new XElement("documentation", item.Documentation),
                        new XElement("insertText", item.InsertText)
                    ))
                );

                ServeResource(context, response.ToString(), "application/xml");
            }
            catch (Exception ex)
            {
                ServeResource(context, $"<error>Failed to process completion request. {ex.Message}</error>", "application/xml", 500);
            }
        }

        private async Task ServeDevTools(HttpListenerContext context, string endpoint)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = "http://localhost:9222" + endpoint;
                    string data = await client.GetStringAsync(url);

                    ServeResource(context, data, "application/json");
                }
            }
            catch (Exception ex)
            {
                ServeResource(context, $"<error>Failed to process DevTools request. {ex.Message}</error>", "application/xml", 500);
            }
        }

        private async Task ServeWhoisRequest(HttpListenerContext context, string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length > 255)
            {
                ServeResource(context, "<error>Invalid query parameter</error>", "application/xml", 400);
                return;
            }

            string whoisServerUrl = "https://xn--c79as89aj0e29b77z.xn--3e0b707e";

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{whoisServerUrl}/kor/whois.jsc")
                {
                    Content = new StringContent($"query={Uri.EscapeDataString(query)}&ip=141.101.82.1", Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                request.Headers.Add("Accept", "*/*");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36 Edg/134.0.3124.77");
                client.DefaultRequestHeaders.Referrer = new Uri($"{whoisServerUrl}/kor/whois/whois.jsp");

                try
                {
                    HttpResponseMessage response = await client.SendAsync(request);
                    string responseBody = await response.Content.ReadAsStringAsync();
                    
                    ServeResource(context, responseBody, "text/plain", (int)response.StatusCode);
                }
                catch (Exception ex)
                {
                    ServeResource(context, $"<error>Failed to process WHOIS request. {ex.Message}</error>", "application/xml", 500);
                }
            }
        }

        private void ServeDnsQueryRequest(HttpListenerContext context, string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length > 255)
            {
                ServeResource(context, "<error>Invalid query parameter</error>", "application/xml", 400);
                return;
            }

            try
            {
                DnsQuery dns = new DnsQuery();
                Dictionary<string, List<string>> allRecords = dns.QueryAll(query);

                StringBuilder result = new StringBuilder();
                foreach (var recordType in allRecords.Keys)
                {
                    result.AppendLine($"\n{recordType} Records:");
                    foreach (var record in allRecords[recordType])
                    {
                        result.AppendLine(record);
                    }
                }

                string data = result.ToString();
                ServeResource(context, data, "text/plain", 200);
            }
            catch (Exception ex)
            {
                ServeResource(context, $"<error>Failed to process DNS query. {ex.Message}</error>", "application/xml", 500);
            }
        }

        private void ServeResource(HttpListenerContext context, byte[] data, string mimeType = "text/html", int statusCode = 200)
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

        private void ServeResource(HttpListenerContext context, string data, string mimeType = "text/html", int statusCode = 200) 
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

    public class CompletionItem
    {
        public string Label { get; set; }
        public string Kind { get; set; }
        public string Documentation { get; set; }
        public string InsertText { get; set; }
    }
}
