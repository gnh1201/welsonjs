// FileEventMonitor.cs
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
using System;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.ServiceProcess;

namespace WelsonJS.Service
{
    public class FileEventMonitor
    {
        private EventLogWatcher eventLogWatcher;
        private ServiceMain parent;
        private enum EventType: int
        {
            FileCreate = 11,
            NetworkConnection = 3,
            RegistryEvent_1 = 12,
            RegistryEvent_2 = 13,
            RegistryEvent_3 = 14
        };
        private enum FileCreateEvent: int {
            RuleName,
            UtcTime,
            ProcessGuid,
            ProcessId,
            Image,
            TargetFilename,
            CreationUtcTime,
            User
        };
        private enum NetworkConnectionEvent: int
        {
            RuleName,
            UtcTime,
            ProcessGuid,
            ProcessId,
            Image,
            User,
            Protocol,
            Initiated,
            SourceIsIpv6,
            SourceIp,
            SourceHostname,
            SourcePort,
            SourcePortName,
            DestinationIsIpv6,
            DestinationIp,
            DestinationHostname,
            DestinationPort,
            DestinationPortName,
        };
        private enum RegistryEvent: int
        {
            RuleName,
            EventType,
            UtcTime,
            ProcessGuid,
            ProcessId,
            Image,
            TargetObject,
            Details,
            User
        }

        public FileEventMonitor(ServiceBase parent, string workingDirectory)
        {
            this.parent = (ServiceMain)parent;
        }

        public void Start()
        {
            try
            {
                string query = @"<QueryList>
                                    <Query Id='0' Path='Microsoft-Windows-Sysmon/Operational'>
                                        <Select Path='Microsoft-Windows-Sysmon/Operational'>*[System/EventID=11 or System/EventID=3]</Select>
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
        }

        private void OnEventRecordWritten(object sender, EventRecordWrittenEventArgs e)
        {
            if (e.EventRecord != null)
            {
                int eventId = e.EventRecord.Id;

                try
                {
                    switch (eventId)
                    {
                        case (int)EventType.FileCreate:
                            {
                                string ruleName = e.EventRecord.Properties[(int)FileCreateEvent.RuleName]?.Value?.ToString();
                                string processId = e.EventRecord.Properties[(int)FileCreateEvent.ProcessId]?.Value?.ToString();
                                string image = e.EventRecord.Properties[(int)FileCreateEvent.Image]?.Value?.ToString();
                                string fileName = e.EventRecord.Properties[(int)FileCreateEvent.TargetFilename]?.Value?.ToString();

                                parent.Log($"> Detected the file creation: {fileName}");
                                parent.Log(parent.DispatchServiceEvent("fileCreated", new string[] {
                                    ruleName,
                                    processId,
                                    image,
                                    fileName
                                }));

                                break;
                            }

                        case (int)EventType.NetworkConnection:
                            {
                                string ruleName = e.EventRecord.Properties[(int)NetworkConnectionEvent.RuleName]?.Value?.ToString();
                                string processId = e.EventRecord.Properties[(int)NetworkConnectionEvent.ProcessId]?.Value?.ToString();
                                string image = e.EventRecord.Properties[(int)NetworkConnectionEvent.Image]?.Value?.ToString();
                                string protocol = e.EventRecord.Properties[(int)NetworkConnectionEvent.Protocol]?.Value?.ToString();
                                string destinationIp = e.EventRecord.Properties[(int)NetworkConnectionEvent.DestinationIp]?.Value?.ToString();
                                string desinationPort = e.EventRecord.Properties[(int)NetworkConnectionEvent.DestinationPort]?.Value?.ToString();
                                string dstinationAddress = $"{protocol}://{destinationIp}:{desinationPort}";

                                parent.Log($"> Detected the network connection: {dstinationAddress}");
                                parent.Log(parent.DispatchServiceEvent("networkConnected", new string[] {
                                    ruleName,
                                    processId,
                                    image,
                                    dstinationAddress
                                }));

                                break;
                            }

                        case (int)EventType.RegistryEvent_1:
                        case (int)EventType.RegistryEvent_2:
                        case (int)EventType.RegistryEvent_3:
                            {
                                string ruleName = e.EventRecord.Properties[(int)RegistryEvent.RuleName]?.Value?.ToString();
                                string processId = e.EventRecord.Properties[(int)RegistryEvent.ProcessId]?.Value?.ToString();
                                string image = e.EventRecord.Properties[(int)RegistryEvent.Image]?.Value?.ToString();
                                string eventType = e.EventRecord.Properties[(int)RegistryEvent.EventType]?.Value?.ToString();
                                string targetObject = e.EventRecord.Properties[(int)RegistryEvent.TargetObject]?.Value?.ToString();

                                parent.Log($"> Detected the registry modification: {targetObject}");
                                parent.Log(parent.DispatchServiceEvent("registryModified", new string[] {
                                    ruleName,
                                    processId,
                                    image,
                                    eventType,
                                    targetObject
                                }));

                                break;
                            }

                        default:
                            throw new ArgumentException("Not supported event type");
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
    }
}