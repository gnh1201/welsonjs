// SerialPortManager.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    public sealed class SerialPortManager : ConnectionManagerBase<SerialPortManager.ConnectionParameters, SerialPort>, IManagedConnectionProvider
    {
        private const string ConnectionTypeName = "Serial Port";

        public struct ConnectionParameters
        {
            public ConnectionParameters(
                string portName,
                int baudRate,
                Parity parity = Parity.None,
                int dataBits = 8,
                StopBits stopBits = StopBits.One,
                Handshake handshake = Handshake.None,
                int readTimeout = 500,
                int writeTimeout = 500,
                int readBufferSize = 1024,
                bool resetBuffersBeforeRequest = false)
            {
                if (string.IsNullOrWhiteSpace(portName)) throw new ArgumentNullException(nameof(portName));

                PortName = portName;
                BaudRate = baudRate;
                Parity = parity;
                DataBits = dataBits;
                StopBits = stopBits;
                Handshake = handshake;
                ReadTimeout = readTimeout;
                WriteTimeout = writeTimeout;
                ReadBufferSize = readBufferSize > 0 ? readBufferSize : 1024;
                ResetBuffersBeforeRequest = resetBuffersBeforeRequest;
            }

            public string PortName { get; }
            public int BaudRate { get; }
            public Parity Parity { get; }
            public int DataBits { get; }
            public StopBits StopBits { get; }
            public Handshake Handshake { get; }
            public int ReadTimeout { get; }
            public int WriteTimeout { get; }
            public int ReadBufferSize { get; }
            public bool ResetBuffersBeforeRequest { get; }
        }

        public SerialPortManager()
        {
            ConnectionMonitorRegistry.RegisterProvider(this);
        }

        public string ConnectionType => ConnectionTypeName;

        protected override string CreateKey(ConnectionParameters parameters)
        {
            return string.Join(",", new object[]
            {
                parameters.PortName.ToUpperInvariant(),
                parameters.BaudRate,
                parameters.Parity,
                parameters.DataBits,
                parameters.StopBits,
                parameters.Handshake,
                parameters.ReadTimeout,
                parameters.WriteTimeout
            });
        }

        protected override Task<SerialPort> OpenConnectionAsync(ConnectionParameters parameters, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var port = new SerialPort(parameters.PortName, parameters.BaudRate, parameters.Parity, parameters.DataBits, parameters.StopBits)
            {
                Handshake = parameters.Handshake,
                ReadTimeout = parameters.ReadTimeout,
                WriteTimeout = parameters.WriteTimeout
            };

            try
            {
                port.Open();
                return Task.FromResult(port);
            }
            catch
            {
                port.Dispose();
                throw;
            }
        }

        protected override bool IsConnectionValid(SerialPort connection)
        {
            return connection != null && connection.IsOpen;
        }

        protected override void CloseConnection(SerialPort connection)
        {
            try
            {
                if (connection != null && connection.IsOpen)
                {
                    connection.Close();
                }
            }
            finally
            {
                connection?.Dispose();
            }
        }

        public Task<TResult> ExecuteAsync<TResult>(
            ConnectionParameters parameters,
            Func<SerialPort, CancellationToken, Task<TResult>> operation,
            int maxAttempts = 2,
            CancellationToken cancellationToken = default)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            return ExecuteWithRetryAsync(parameters, operation, maxAttempts, cancellationToken);
        }

        public async Task<string> SendAndReceiveAsync(
            ConnectionParameters parameters,
            string message,
            Encoding encoding,
            CancellationToken cancellationToken = default)
        {
            if (encoding == null) throw new ArgumentNullException(nameof(encoding));
            byte[] payload = encoding.GetBytes(message ?? string.Empty);

            return await ExecuteWithRetryAsync(
                parameters,
                (port, token) => SendAndReceiveInternalAsync(
                    port,
                    parameters.ReadBufferSize,
                    payload,
                    encoding,
                    parameters.ResetBuffersBeforeRequest,
                    token),
                2,
                cancellationToken).ConfigureAwait(false);
        }

        public IReadOnlyCollection<ManagedConnectionStatus> GetStatuses()
        {
            var snapshots = SnapshotConnections();
            var result = new List<ManagedConnectionStatus>(snapshots.Count);

            foreach (var snapshot in snapshots)
            {
                string state;
                try
                {
                    state = snapshot.Connection?.IsOpen == true ? "Open" : "Closed";
                }
                catch
                {
                    state = "Unknown";
                }

                var parameters = snapshot.Parameters;
                string description = $"{parameters.PortName} @ {parameters.BaudRate} bps";

                result.Add(new ManagedConnectionStatus(
                    ConnectionTypeName,
                    snapshot.Key,
                    state,
                    description,
                    snapshot.IsValid));
            }

            return result;
        }

        public bool TryClose(string key)
        {
            return TryRemoveByKey(key);
        }

        private static async Task<string> SendAndReceiveInternalAsync(
            SerialPort port,
            int bufferSize,
            byte[] payload,
            Encoding encoding,
            bool resetBuffers,
            CancellationToken token)
        {
            if (resetBuffers)
            {
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
            }

            if (payload.Length > 0)
            {
                await Task.Run(() => port.Write(payload, 0, payload.Length), token).ConfigureAwait(false);
            }

            using (var stream = new MemoryStream())
            {
                var buffer = new byte[bufferSize];

                while (true)
                {
                    try
                    {
                        int read = await Task.Run(() => port.Read(buffer, 0, buffer.Length), token).ConfigureAwait(false);
                        if (read > 0)
                        {
                            stream.Write(buffer, 0, read);
                            if (port.BytesToRead == 0)
                            {
                                break;
                            }
                        }
                        else
                        {
                            if (port.BytesToRead == 0)
                            {
                                break;
                            }
                        }
                    }
                    catch (TimeoutException)
                    {
                        break;
                    }
                }

                return encoding.GetString(stream.ToArray());
            }
        }
    }
}
