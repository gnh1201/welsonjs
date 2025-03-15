using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

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
            Process.Start(_prefix);

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
                    ProcessRequest(await _listener.GetContextAsync());
                }
                catch (Exception ex)
                {
                    if (token.IsCancellationRequested || !_isRunning) break;
                    MessageBox.Show($"Error: {ex.Message}");
                }
            }
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            string path = context.Request.Url.AbsolutePath.TrimStart('/');

            // Serve the favicon.ico file
            if ("favicon.ico".Equals(path, StringComparison.OrdinalIgnoreCase))
            {
                ServeResource(context, GetResource("favicon"), "image/x-icon");
                return;
            }

            // Serve the code completion (word suggestion)
            if (path.StartsWith("completion/", StringComparison.OrdinalIgnoreCase))
            {
                ServeCompletion(context, path.Substring("completion/".Length));
                return;
            }

            // Serve a resource
            ServeResource(context, GetResource(_resourceName), "text/html");
        }

        private void ServeCompletion(HttpListenerContext context, string word)
        {
            int statusCode = 200;

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
                        Documentation = "An executable file",
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

                byte[] data = Encoding.UTF8.GetBytes(
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                    response.ToString()
                );

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/xml";
                context.Response.ContentLength64 = data.Length;
                using (Stream outputStream = context.Response.OutputStream)
                {
                    outputStream.Write(data, 0, data.Length);
                }
            }
            catch (Exception ex)
            {
                byte[] errorData = Encoding.UTF8.GetBytes(
                    "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" +
                    $"<error>Failed to process completion request. {ex.Message}</error>"
                );
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/xml";
                context.Response.ContentLength64 = errorData.Length;
                using (Stream outputStream = context.Response.OutputStream)
                {
                    outputStream.Write(errorData, 0, errorData.Length);
                }
            }
        }

        private void ServeResource(HttpListenerContext context, byte[] data, string mimeType = "text/html")
        {
            int statusCode = 200;

            if (data == null) {
                data = "text/html".Equals(mimeType, StringComparison.OrdinalIgnoreCase) ?
                    Encoding.UTF8.GetBytes("<html><body><h1>Could not find the resource.</h1></body></html>") :
                    Encoding.UTF8.GetBytes("Could not find the resource.")
                ;
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

        private byte[] GetResource(string resourceName)
        {
            // Try to fetch embedded resource.
            byte[] data = GetEmbeddedResource(typeof(ResourceServer).Namespace + "." + resourceName);
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
