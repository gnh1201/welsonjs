// FileEventMonitor.cs
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
using System;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using libyaraNET;
using System.Collections.Generic;
using System.ServiceProcess;
using WelsonJS.Service.Model;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using System.Security;

namespace WelsonJS.Service
{
    public class FileEventMonitor
    {
        private Rules rules;
        private EventLogWatcher eventLogWatcher;
        private ServiceMain parent;
        private string ruleDirectoryPath;
        private enum EventType11: int {
            RuleName,
            UtcTime,
            ProcessGuid,
            ProcessId,
            Image,
            TargetFilename,
            CreationUtcTime,
            User
        };

        public FileEventMonitor(ServiceBase parent, string workingDirectory)
        {
            this.parent = (ServiceMain)parent;
            ruleDirectoryPath = Path.Combine(workingDirectory, "app/assets/yar");

            try
            {
                AddYaraRulesFromDirectory(ruleDirectoryPath);
            }
            catch (Exception ex)
            {
                this.parent.Log($"Failed to read the rules: {ex.Message}");
            }
        }

        public void AddYaraRulesFromDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                throw new FileNotFoundException($"{directoryPath} directory not found.");
            }

            var ruleFiles = Directory.GetFiles(directoryPath, "*.yar");
            AddYaraRules(new List<string>(ruleFiles));
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
                                parent.Log($"Added the rule: {ruleFile}");
                            }
                            else
                            {
                                throw new FileNotFoundException($"{ruleFile} file not found.");
                            }
                        }

                        rules = compiler.GetRules();
                    }
                }
                catch (Exception ex)
                {
                    parent.Log($"Error adding the rules: {ex.Message}");
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
                parent.Log($"Failed to connect the Windows EventLog Service: {ex.Message}");
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
                    string fileName = e.EventRecord.Properties[(int)EventType11.TargetFilename]?.Value?.ToString();

                    if (string.IsNullOrEmpty(fileName))
                    {
                        throw new ArgumentException("Could not read the target filename.");
                    }

                    if (File.Exists(fileName))
                    {
                        parent.Log($"File created: {fileName}");
                        parent.DispatchServiceEvent("fileCreated", new string[] { fileName });
                    }
                    else
                    {
                        throw new FileNotFoundException($"{fileName} file not found.");
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

        private void CheckFile(string filePath)
        {
            if (rules == null)
            {
                throw new ArgumentNullException("No rules added. Skipping check the file.");
            }

            using (var ctx = new YaraContext())
            {
                var scanner = new Scanner();
                var results = scanner.ScanFile(filePath, rules);

                if (results.Count > 0)
                {
                    parent.Log($"Match Found: {filePath}");

                    foreach (var result in results)
                    {
                        Dictionary<string, List<Match>> matches = result.Matches;
                        foreach (KeyValuePair<string, List<Match>> match in matches)
                        {
                            string ruleName = match.Key;
                            List<Match> ruleMatches = match.Value;
                            ruleMatches.ForEach((x) =>
                            {
                                parent.Log($"Matched {ruleName}: {filePath}, Offset {x.Offset}");
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
                    parent.Log($"No match found in {filePath}.");
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