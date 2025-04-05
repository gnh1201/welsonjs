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
        private List<IResourceTool> _resourceTools = new List<IResourceTool>();

        public ResourceServer(string prefix, string resourceName)
        {
            _prefix = prefix;
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _resourceName = resourceName;

            // Add resource tools
            _resourceTools.Add(new ResourceTools.Completion(this));
            _resourceTools.Add(new ResourceTools.Config(this));
            _resourceTools.Add(new ResourceTools.DevTools(this));
            _resourceTools.Add(new ResourceTools.Tfa(this));
            _resourceTools.Add(new ResourceTools.Whois(this));
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

            // Serve from a resource tool
            foreach(var tool in _resourceTools)
            {

               if (tool.CanHandle(path))
                {
                    await tool.HandleAsync(context, path);
                    return;
                }
            }

            // Serve from a resource name
            ServeResource(context, GetResource(_resourceName), "text/html");
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
