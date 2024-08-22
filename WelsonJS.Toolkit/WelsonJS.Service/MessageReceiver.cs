// MessageReceiver.cs
// https://github.com/gnh1201/welsonjs
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Management;
using System.ServiceProcess;
using System.Threading.Tasks;
using WelsonJS.GrpcService;

namespace WelsonJS.Service
{
    public class MessageReceiver
    {
        private GrpcChannel channel;
        private ServiceMain parent;
        private string deviceId;
        private string serverAddress;

        public MessageReceiver(ServiceBase parent, string workingDirectory)
        {
            this.parent = (ServiceMain)parent;

            // Read the device ID on this computer
            deviceId = GetSystemUUID();

            // Read configuration from settings.ini
            if (!String.IsNullOrEmpty(deviceId))
            {
                this.parent.Log($"Resolved the device ID: {deviceId}");

                try
                {
                    serverAddress = this.parent.GetSettingsFileHandler().Read("GRPC_HOST", "Service");
                    if (String.IsNullOrEmpty(serverAddress))
                    {
                        throw new Exception("The server addresss is empty.");
                    }
                }
                catch (Exception ex)
                {
                    serverAddress = "http://localhost:50051";
                    this.parent.Log($"Failed to read the server address. {ex.Message} Use default value: {serverAddress}");
                }
            }
            else
            {
                serverAddress = null;
                this.parent.Log($"Failed to resolve the device ID");
            }

            // Set the GRPC channel
            if (serverAddress != null)
            {
                try
                {
                    this.parent.Log($"Use the remote address: {serverAddress}");
                    channel = GrpcChannel.ForAddress(serverAddress);
                }
                catch (Exception ex)
                {
                    channel = null;
                    this.parent.Log($"Failed to initialize the GRPC channel: {ex.Message}");
                }
            }
        }
        
        public void Start()
        {
            if (channel != null)
            {
                Task.Run(() => GetTask());
                parent.Log("GRPC Message Receiver Started");
            }
            else
            {
                parent.Log("Failed to initalize the GRPC channel");
            }
        }

        private async Task GetTask()
        {
            parent.Log("Use the device ID: " + deviceId);

            try
            {
                var client = new MessageController.MessageControllerClient(channel);

                var request = new MessageRequest {
                    ClientId = deviceId
                };
                var call = client.SendMessageStream(request);

                while (await call.ResponseStream.MoveNext())
                {
                    var response = call.ResponseStream.Current;
                    parent.Log($"< {response.Message}");

                    // dispatch to the script runtime
                    parent.DispatchServiceEvent("messageReceived", new string[] { response.Message });
                }
            }
            finally
            {
                channel?.Dispose();
            }
        }

        private string GetSystemUUID()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct"))
                {
                    foreach (var mo in searcher.Get())
                    {
                        return mo["UUID"].ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                parent.Log($"An error occurred while retrieving the system UUID: {ex.Message}");
            }

            return null;
        }
    }
}
