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
                serverAddress = _parent.GetSettingsHandler().Read("GRPC_HOST", "Service");
                if (String.IsNullOrEmpty(serverAddress))
                {
                    throw new Exception("The server address could not be empty.");
                }
            }
            catch (Exception ex)
            {
                serverAddress = "http://localhost:50051";
                _parent.Log($"Failed to read the address because of {ex.Message}. Set default: {serverAddress}");
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
            while (true)
            {
                var call = _client.CheckHeartbeat();
                try
                {

                    var request = new HeartbeatRequest
                    {
                        IsAlive = true
                    };

                    await call.RequestStream.WriteAsync(request);
                    await call.RequestStream.CompleteAsync();
                    _parent.Log("Sent heartbeat");

                    await Task.Delay(HeartbeatInterval); // HeartbeatInterval 동안 대기

                }
                catch (Exception ex)
                {
                    _parent.Log("Heartbeat request stream failed: " + ex.Message);
                }

                // 서버 응답을 수신하는 작업
                try
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        var response = call.ResponseStream.Current;
                        _parent.Log("Heartbeat response received: " + response.IsAlive);
                    }
                }
                catch (RpcException ex)
                {
                    _parent.Log($"gRPC error: {ex.Status.Detail}");
                }
                catch (Exception ex)
                {
                    _parent.Log($"Unexpected error: {ex.Message}");
                }
                finally
                {
                    // 잠시 대기 후 재연결
                    await Task.Delay(TimeSpan.FromMilliseconds(HeartbeatInterval));
                }
            }
        }

        public async Task StartEventListenerAsync()
        {
            var eventRequest = new FetchEventsRequest
            {
                ClientId = clientId
            };
            while (true)
            {
                var eventCall = _client.FetchPendingEvents(eventRequest);
                try
                {
                    while (await eventCall.ResponseStream.MoveNext())
                    {
                        var response = eventCall.ResponseStream.Current;
                        _parent.Log($"Received event from server: {response.EventType} with args: {string.Join(", ", response.Args)}");
                    }
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(HeartbeatInterval));
                }
            }

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
