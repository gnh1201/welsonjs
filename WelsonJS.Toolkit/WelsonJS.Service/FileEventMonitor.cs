// FileEventMonitor.cs
// https://github.com/gnh1201/welsonjs
using System;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using libyaraNET;
using System.Collections.Generic;
using System.ServiceProcess;

namespace WelsonJS.Service
{
    public class FileEventMonitor
    {
        private Rules rules;
        private EventLogWatcher eventLogWatcher;
        private ServiceMain parent;
        private string ruleFolderPath;

        public FileEventMonitor(ServiceBase parent, string workingDirectory)
        {
            this.parent = (ServiceMain)parent;
            ruleFolderPath = Path.Combine(workingDirectory, "app/assets/yar");

            try
            {
                AddYaraRules(new List<string>(Directory.GetFiles(ruleFolderPath, "*.yar")));
            }
            catch (Exception ex)
            {
                this.parent.Log($"Exception (FileEventMonitor): {ex.Message}");
            }
        }

        public void AddYaraRulesFromDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Directory not found: {directoryPath}");
                return;
            }

            var yarFiles = Directory.GetFiles(directoryPath, "*.yar");
            AddYaraRules(new List<string>(yarFiles));
        }

        public void AddYaraRules(List<string> ruleFiles)
        {
            Dispose();

            using (var ctx = new YaraContext())
            {
                try
                {
                    using (var compiler = new Compiler())
                    {
                        foreach (var ruleFile in ruleFiles)
                        {
                            if (File.Exists(ruleFile))
                            {
                                compiler.AddRuleFile(ruleFile);
                                parent.Log($"Loaded YARA rule from {ruleFile}");
                            }
                            else
                            {
                                parent.Log($"File not found: {ruleFile}");
                            }
                        }

                        rules = compiler.GetRules();
                    }
                }
                catch (Exception ex)
                {
                    parent.Log($"Error loading YARA rules: {ex.Message}");
                }
            }
        }

        public void Start()
        {
            try
            {
                string query = @"<QueryList>
                                    <Query Id='0' Path='Microsoft-Windows-Sysmon/Operational'>
                                        <Select Path='Microsoft-Windows-Sysmon/Operational'>*[System/EventID=11]</Select>
                                    </Query>
                                 </QueryList>";

                EventLogQuery eventLogQuery = new EventLogQuery("Microsoft-Windows-Sysmon/Operational", PathType.LogName, query);
                eventLogWatcher = new EventLogWatcher(eventLogQuery);

                eventLogWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(OnEventRecordWritten);
                eventLogWatcher.Enabled = true;
            }
            catch (Exception ex)
            {
                parent.Log($"Exception (FileEventMonitor): {ex.Message}");
                Stop();
            }
        }

        public void Stop()
        {
            if (eventLogWatcher != null)
            {
                eventLogWatcher.Dispose();
                eventLogWatcher = null;
            }

            Dispose();
        }

        private void OnEventRecordWritten(object sender, EventRecordWrittenEventArgs e)
        {
            if (e.EventRecord != null)
            {
                try
                {
                    string fileName = e.EventRecord.Properties[7]?.Value?.ToString();
                    if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
                    {
                        parent.Log($"File created: {fileName}");
                        parent.DispatchServiceEvent("fileCreated", new string[] { fileName });
                        ScanFileWithYara(fileName);
                    }
                }
                catch (Exception ex)
                {
                    parent.Log($"Error processing event: {ex.Message}");
                }
            }
            else
            {
                parent.Log("The event instance was null.");
            }
        }

        private void ScanFileWithYara(string filePath)
        {
            if (rules == null)
            {
                parent.Log("No YARA rules loaded. Skipping file scan.");
                return;
            }

            using (var ctx = new YaraContext())
            {
                var scanner = new Scanner();
                var results = scanner.ScanFile(filePath, rules);

                if (results.Count > 0)
                {
                    parent.Log($"YARA match found in file {filePath}:");

                    foreach (var result in results)
                    {
                        var matches = result.Matches;
                        foreach (var match in matches)
                        {
                            parent.Log($"YARA matched: {match.ToString()}");

                            parent.DispatchServiceEvent("fileRuleMatched", new string[] { filePath, match.ToString() });
                        }
                    }
                }
                else
                {
                    parent.Log($"No YARA match found in file {filePath}.");
                }
            }
        }

        private void Dispose()
        {
            if (rules != null)
            {
                rules.Dispose();
                rules = null;
            }
        }
    }
}