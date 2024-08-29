// HeartbeatClient.cs
// Namhyeon Go <abuse@catswords.net>
// https://github.com/gnh1201/welsonjs
using Grpc.Core;
using System.Threading.Tasks;
using System;
using System.Management;
using System.ServiceProcess;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using System.Net.Http;

namespace WelsonJS.Service
{
    public class HeartbeatClient
    {
        private readonly HeartbeatService.HeartbeatServiceClient _client;
        private readonly GrpcChannel _channel;
        private const int HeartbeatInterval = 2000; // 2 seconds
        private ServiceMain _parent;
        private string clientId;
        private string serverAddress;

        public HeartbeatClient(ServiceBase parent)
        {
            _parent = (ServiceMain)parent;

            try
            {
                serverAddress = _parent.GetSettingsFileHandler().Read("GRPC_HOST", "Service");
                if (String.IsNullOrEmpty(serverAddress))
                {
                    throw new Exception("The server address could not be empty.");
                }
            }
            catch (Exception ex)
            {
                serverAddress = "http://localhost:50051";
                _parent.Log($"Failed to read the host address. {ex.Message} Use default value: {serverAddress}");
            }

            var httpClientHandler = new HttpClientHandler();
            var grpcWebHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, httpClientHandler);
            _channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions
            {
                HttpHandler = grpcWebHandler,
                Credentials = ChannelCredentials.Insecure
            });
            _client = new HeartbeatService.HeartbeatServiceClient(_channel);

            clientId = GetSystemUUID().ToLower();
            _parent.Log($"Use the client ID: {clientId}");
        }

        public async Task StartHeartbeatAsync()
        {
            var call = _client.CheckHeartbeat();
            try
            {
                while (true)
                {
                    var request = new HeartbeatRequest {
                        IsAlive = true
                    };
                    await call.RequestStream.WriteAsync(request);
                    _parent.Log("Sent heartbeat");

                    if (await call.ResponseStream.MoveNext())
                    {
                        var response = call.ResponseStream.Current;
                        _parent.Log("Heartbeat response received: " + response.IsAlive);
                    }

                    await Task.Delay(HeartbeatInterval);
                }
            }
            finally
            {
                await call.RequestStream.CompleteAsync();
            }
        }

        public async Task StartEventListenerAsync()
        {
            var eventRequest = new FetchEventsRequest {
                ClientId = clientId
            };
            var eventCall = _client.FetchPendingEvents(eventRequest);
            try
            {
                while (await eventCall.ResponseStream.MoveNext())
                {
                    var response = eventCall.ResponseStream.Current;
                    _parent.Log($"Received event from server: {response.EventType} with args: {string.Join(", ", response.Args)}");
                }
            }
            finally {}
        }

        public async Task ShutdownAsync()
        {
            await _channel.ShutdownAsync();
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
                _parent.Log($"An error occurred while retrieving the system UUID: {ex.Message}");
            }

            return "UNKNOWN";
        }
    }
}
