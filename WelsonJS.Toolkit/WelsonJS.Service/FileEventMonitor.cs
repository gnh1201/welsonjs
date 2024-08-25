// FileEventMonitor.cs
// https://github.com/gnh1201/welsonjs
using System;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using libyaraNET;
using System.Collections.Generic;
using System.ServiceProcess;
using WelsonJS.Service.Model;

namespace WelsonJS.Service
{
    public class FileEventMonitor
    {
        private Rules rules;
        private EventLogWatcher eventLogWatcher;
        private ServiceMain parent;
        private string ruleDirectoryPath;

        public FileEventMonitor(ServiceBase parent, string workingDirectory)
        {
            this.parent = (ServiceMain)parent;
            ruleDirectoryPath = Path.Combine(workingDirectory, "app/assets/yar");

            try
            {
                AddYaraRules(new List<string>(Directory.GetFiles(ruleDirectoryPath, "*.yar")));
            }
            catch (Exception ex)
            {
                this.parent.Log($"Failed to read the rule files: {ex.Message}");
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
                                parent.Log($"Loaded the rule from {ruleFile}");
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
                    parent.Log($"Error loading the rules: {ex.Message}");
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
            }
        }

        public void Stop()
        {
            if (eventLogWatcher != null)
            {
                try
                {
                    eventLogWatcher.Dispose();
                    eventLogWatcher = null;
                }
                catch (Exception)
                {
                    eventLogWatcher = null;
                }
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
                parent.Log("No rules loaded. Skipping file scan.");
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
                        Dictionary<string, List<Match>> matches = result.Matches;
                        foreach (KeyValuePair<string, List<Match>> match in matches)
                        {
                            string ruleName = match.Key;
                            List<Match> ruleMatches = match.Value;
                            ruleMatches.ForEach((x) =>
                            {
                                parent.Log($"Rule matched: {ruleName}, {filePath}, Offset {x.Offset}");
                                parent.DispatchServiceEvent("fileRuleMatched", new string[] { ruleName, filePath, x.Offset.ToString() });

                                IndexFileRuleMatched(new FileMatchResult
                                {
                                    FilePath = filePath,
                                    Offset = x.Offset,
                                    RuleName = ruleName,
                                    LastChecked = DateTime.Now
                                });
                            });
                        }
                    }
                }
                else
                {
                    parent.Log($"No match found in file {filePath}.");
                }
            }
        }

        private void IndexFileRuleMatched(FileMatchResult match)
        {
            // TODO (Save a result to the document indexer)
        }

        private void Dispose()
        {
            if (rules != null)
            {
                try
                {
                    //rules.Dispose();
                    rules = null;
                }
                catch (Exception)
                {
                    rules = null;
                }
            }
        }
    }
}