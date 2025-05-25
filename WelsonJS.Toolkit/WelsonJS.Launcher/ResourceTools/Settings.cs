// Settings.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
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
                if (entry.Value is string strValue)
                {
                    resourceStrings[(string)entry.Key] = strValue;
                }
            }

            // Load settings from app.config
            var appConfig = ConfigurationManager.AppSettings.AllKeys
                .ToDictionary(k => k, k => ConfigurationManager.AppSettings[k]);

            // Merge by starting with resourceStrings and letting app.config override
            var finalConfig = new Dictionary<string, string>(resourceStrings);
            foreach (var kv in appConfig)
            {
                finalConfig[kv.Key] = kv.Value;
            }

            // Generate XML using XElement
            XElement xml = new XElement("settings",
                finalConfig.Select(kv => new XElement(kv.Key, kv.Value))
            );

            Server.ServeResource(context, xml.ToString(), "application/xml");
        }
    }
}
