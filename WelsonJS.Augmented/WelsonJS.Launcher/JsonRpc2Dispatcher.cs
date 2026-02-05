using log4net;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    public sealed class JsonRpc2Request
    {
        public string Version;
        public string Method;
    }

    public sealed class JsonRpc2Exception : Exception
    {
        public JsonRpc2Exception(string message) : base(message) { }
        public JsonRpc2Exception(string message, Exception inner) : base(message, inner) { }
    }

    public sealed class JsonRpc2Dispatcher
    {
        private readonly ILog _logger;

        public JsonRpc2Dispatcher(ILog logger)
        {
            _logger = logger;
        }

        public async Task<string> HandleAsync(
            string requestBody,
            Func<string, JsSerializer, CancellationToken, Task<string>> dispatchMethodAsync,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(requestBody))
                throw new JsonRpc2Exception("Empty request body");

            if (dispatchMethodAsync == null)
                throw new ArgumentNullException("dispatchMethodAsync");

            using (var ser = new JsSerializer())
            {
                int id = ser.Load(requestBody);

                try
                {
                    string version = ser.ExtractFrom(id, "jsonrpc");
                    if (!string.Equals(version, "2.0", StringComparison.Ordinal))
                        throw new JsonRpc2Exception("Unsupported jsonrpc version: " + version);

                    string method = ser.ExtractFrom(id, "method");
                    if (string.IsNullOrEmpty(method))
                        throw new JsonRpc2Exception("Missing method");

                    var req = new JsonRpc2Request
                    {
                        Version = version,
                        Method = method
                    };

                    return await dispatchMethodAsync(req.Method, ser, ct);
                }
                catch (JsonRpc2Exception)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (_logger != null)
                        _logger.Error("[JsonRpc2] Parse error", ex);

                    throw new JsonRpc2Exception("Parse error", ex);
                }
                finally
                {
                    ser.Unload(id);
                }
            }
        }
    }
}