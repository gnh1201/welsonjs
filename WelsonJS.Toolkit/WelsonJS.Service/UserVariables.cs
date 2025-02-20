using System.Collections.Generic;
using System;
using System.IO;
using System.ServiceProcess;

namespace WelsonJS.Service
{
    public class UserVariables
    {
        private ServiceMain parent;
        private Dictionary<string, string> userVariables;
        private string envFilePath;

        public UserVariables(ServiceBase parent)
        {
            envFilePath = Path.Combine(Program.GetAppDataPath(), "welsonjs_default.env");
            this.parent = (ServiceMain)parent;
        }

        // Load user-defined variables from the temporary folder in .env format
        public void Load()
        {
            if (File.Exists(envFilePath))
            {
                try
                {
                    string fileContent = File.ReadAllText(envFilePath);
                    // Split based on new line characters
                    string[] keyValuePairs = fileContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    userVariables = new Dictionary<string, string>();

                    foreach (string pair in keyValuePairs)
                    {
                        // Split by the first occurrence of '='
                        int indexOfEquals = pair.IndexOf('=');
                        if (indexOfEquals != -1)
                        {
                            string key = pair.Substring(0, indexOfEquals).Trim();
                            string value = pair.Substring(indexOfEquals + 1).Trim();

                            // Remove surrounding quotes if present
                            if (value.StartsWith("\"") && value.EndsWith("\""))
                            {
                                value = value.Substring(1, value.Length - 2); // Remove the first and last character
                            }

                            // Unescape double quotes in the value
                            value = value.Replace("\\\"", "\"");

                            userVariables[key] = value;
                        }
                        else
                        {
                            throw new Exception($"Error parsing line: '{pair}'.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading variable file: {ex.Message}");
                    userVariables = new Dictionary<string, string>();
                }
            }
            else
            {
                userVariables = new Dictionary<string, string>();
            }
        }

        public string GetValue(string name)
        {
            userVariables.TryGetValue(name, out string value);
            return value;
        }

        public string GetEnvFilePath()
        {
            return envFilePath;
        }
    }
}
