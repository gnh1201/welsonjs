// HeartbeatClient.cs
// SPDX-License-Identifier: MS-RL
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using Grpc.Core;
using System.Threading.Tasks;
using System;
using System.Management;
using System.ServiceProcess;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace WelsonJS.Service
{
    public class HeartbeatClient
    {
        private readonly HeartbeatService.HeartbeatServiceClient _client;
        private ILogger logger;
        private readonly GrpcChannel _channel;
        private int HeartbeatInterval;
        private ServiceMain parent;
        private string clientId;
        private string serverAddress;

        public HeartbeatClient(ServiceBase _parent, ILogger _logger)
        {
            parent = (ServiceMain)_parent;
            logger = _logger;

            HeartbeatInterval = int.Parse(parent.ReadSettingsValue("HEARTBEAT_INTERVAL") ?? "2000");

            try
            {
                serverAddress = parent.ReadSettingsValue("GRPC_HOST");
                if (String.IsNullOrEmpty(serverAddress))
                {
                    throw new Exception("The server address could not be empty.");
                }
            }
            catch (Exception ex)
            {
                serverAddress = "http://localhost:50051";
                logger.LogInformation($"Failed to read the address because of {ex.Message}. Set default: {serverAddress}");
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
            logger.LogInformation($"Use the client ID: {clientId}");
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
                    logger.LogInformation("Sent heartbeat");

                    await Task.Delay(HeartbeatInterval); // Wait for HeartbeatInterval

                }
                catch (Exception ex)
                {
                    logger.LogInformation("Heartbeat request stream failed: " + ex.Message);
                }

                // 서버 응답을 수신하는 작업
                try
                {
                    while (await call.ResponseStream.MoveNext())
                    {
                        var response = call.ResponseStream.Current;
                        logger.LogInformation("Heartbeat response received: " + response.IsAlive);
                    }
                }
                catch (RpcException ex)
                {
                    logger.LogInformation($"gRPC error: {ex.Status.Detail}");
                }
                catch (Exception ex)
                {
                    logger.LogInformation($"Unexpected error: {ex.Message}");
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
                        logger.LogInformation($"Received event from server: {response.EventType} with args: {string.Join(", ", response.Args)}");
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
                logger.LogInformation($"An error occurred while retrieving the system UUID: {ex.Message}");
            }

            return "UNKNOWN";
        }
    }
}
