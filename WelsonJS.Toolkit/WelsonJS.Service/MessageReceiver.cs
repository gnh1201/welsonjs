// MessageReceiver.cs
// https://github.com/gnh1201/welsonjs
using DeviceId;
using Grpc.Core;
using Grpc.Net.Client;
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

        public MessageReceiver(ServiceBase parent, string workingDirectory)
        {
            this.parent = (ServiceMain)parent;

            // Read the device ID on this computer
            deviceId = new DeviceIdBuilder()
                .OnWindows(windows => windows.AddWindowsDeviceId())
                .ToString();

            // Read configuration from settings.ini
            try
            {
                // Get the GRPC server URL from settings
                string grpcServerAddress = this.parent.GetSettingsFileHandler().Read("GRPC_SERVER_ADDRESS");

                // Set the GRPC channel
                channel = GrpcChannel.ForAddress(grpcServerAddress);
            }
            catch
            {
                channel = null;
            }
        }
        
        public void Start()
        {
            if (channel != null)
            {
                Task.Run(() => GetTask());
            }
            else
            {
                parent.Log("Not Initializd GRPC channel");
            }
            
        }

        private async Task GetTask()
        {
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
                    parent.Log($"Received: {response.Message}");
                }
            }
            finally
            {
                channel?.Dispose();
            }
        }
    }
}
