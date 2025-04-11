using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace WelsonJS.Launcher.ResourceTools
{
    public class Settings : IResourceTool
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private const string Prefix = "settings";

        public Settings(ResourceServer server, HttpClient httpClient)
        {
            Server = server;
            _httpClient = httpClient;
        }

        public bool CanHandle(string path)
        {
            return path.Equals(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            await Task.Delay(0);
            
            // Get current namespace (e.g., WelsonJS.Launcher)
            string ns = typeof(Program).Namespace;

            // Build resource base name (e.g., WelsonJS.Launcher.Properties.Resources)
            string resourceBaseName = ns + ".Properties.Resources";

            // Load resource strings using ResourceManager
            var resourceManager = new ResourceManager(resourceBaseName, Assembly.GetExecutingAssembly());
            var resourceSet = resourceManager.GetResourceSet(
                System.Globalization.CultureInfo.CurrentCulture,
                true,
                true
            );

            var resourceStrings = new Dictionary<string, string>();
            foreach (System.Collections.DictionaryEntry entry in resourceSet)
            {
                string key = (string)entry.Key;
                object value = entry.Value;

                if (value is string strValue)
                {
                    resourceStrings[key] = strValue;
                }
            }

            // Load settings from app.config
            var appConfig = ConfigurationManager.AppSettings.AllKeys
                .ToDictionary(k => k, k => ConfigurationManager.AppSettings[k]);

            // Merge keys from both sources (app.config has priority)
            var allKeys = new HashSet<string>(resourceStrings.Keys.Concat(appConfig.Keys));
            var finalConfig = new Dictionary<string, string>();

            foreach (var key in allKeys)
            {
                if (appConfig.ContainsKey(key))
                    finalConfig[key] = appConfig[key];
                else if (resourceStrings.ContainsKey(key))
                    finalConfig[key] = resourceStrings[key];
            }

            // Generate XML using XElement
            XElement xml = new XElement("settings",
                finalConfig.Select(kv => new XElement(kv.Key, kv.Value))
            );

            Server.ServeResource(context, xml.ToString(), "application/xml");
        }
    }
}
