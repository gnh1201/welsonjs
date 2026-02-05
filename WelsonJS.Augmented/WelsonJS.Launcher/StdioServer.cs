// StdioServer.cs
// Minimal stdio server (Content-Length framing) with delegate-based extension.
// - Handles ONLY stdio framing (read/write message boundaries)
// - No JSON parsing/formatting
// - Sequential processing
// - Cancellation via Ctrl+C, and EOF handling
//
// Delegate contract:
//   - input: raw UTF-8 payload bytes (exactly Content-Length)
//   - output: raw UTF-8 payload bytes to write (or null/empty to write nothing)
//
// Typical use: plug JSON-RPC/MCP dispatcher outside of this class.
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    public sealed class StdioServer
    {
        public delegate Task<byte[]> Handler(byte[] payload, CancellationToken ct);

        private readonly Stream _inStream;
        private readonly Stream _outStream;
        private readonly Handler _handler;

        public StdioServer(Handler handler)
        {
            if (handler == null)
                throw new ArgumentNullException("handler");

            _handler = handler;
            _inStream = Console.OpenStandardInput();
            _outStream = Console.OpenStandardOutput();
        }

        public void Run()
        {
            using (var cts = new CancellationTokenSource())
            {
                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };

                RunAsync(cts.Token).GetAwaiter().GetResult();
            }
        }

        public async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                byte[] payload;

                // 1) read one framed message (blocks here waiting for stdin)
                try
                {
                    payload = await ReadOneAsync(ct).ConfigureAwait(false);
                    if (payload == null) return; // EOF => exit
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch
                {
                    // framing broken or stream error => stop (or continue if you want resync)
                    return;
                }

                // 2) handle + write response (never kill the loop on handler failure)
                try
                {
                    var resp = await _handler(payload, ct).ConfigureAwait(false);
                    if (resp == null) resp = new byte[0];

                    await WriteOneAsync(resp, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch
                {
                    // keep listening even if handler fails
                    // optionally write empty response so client doesn't hang waiting
                    try { await WriteOneAsync(new byte[0], ct).ConfigureAwait(false); } catch { }
                    continue;
                }
            }
        }

        private async Task<byte[]> ReadOneAsync(CancellationToken ct)
        {
            // Read headers until \r\n\r\n (blocks on stdin)
            string headers = await ReadHeadersAsync(ct).ConfigureAwait(false);
            if (headers == null) return null; // EOF

            int contentLength = ParseContentLength(headers);
            if (contentLength < 0) throw new InvalidDataException("Missing Content-Length");

            return await ReadExactAsync(_inStream, contentLength, ct).ConfigureAwait(false);
        }

        private async Task WriteOneAsync(byte[] payload, CancellationToken ct)
        {
            if (payload == null) payload = new byte[0];

            string header = "Content-Length: " + payload.Length.ToString(CultureInfo.InvariantCulture) + "\r\n\r\n";
            byte[] headerBytes = Encoding.ASCII.GetBytes(header);

            await _outStream.WriteAsync(headerBytes, 0, headerBytes.Length, ct).ConfigureAwait(false);
            if (payload.Length > 0)
                await _outStream.WriteAsync(payload, 0, payload.Length, ct).ConfigureAwait(false);

            await _outStream.FlushAsync(ct).ConfigureAwait(false);
        }

        private async Task<string> ReadHeadersAsync(CancellationToken ct)
        {
            // read byte-by-byte until CRLFCRLF
            var buf = new byte[4096];
            int len = 0;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                int b = await ReadByteAsync(_inStream, ct).ConfigureAwait(false);
                if (b < 0)
                {
                    if (len == 0) return null; // clean EOF
                    throw new EndOfStreamException("EOF while reading headers");
                }

                if (len == buf.Length)
                {
                    // grow
                    var nb = new byte[buf.Length * 2];
                    Buffer.BlockCopy(buf, 0, nb, 0, buf.Length);
                    buf = nb;
                }

                buf[len++] = (byte)b;

                if (len >= 4 &&
                    buf[len - 4] == 13 &&
                    buf[len - 3] == 10 &&
                    buf[len - 2] == 13 &&
                    buf[len - 1] == 10)
                {
                    return Encoding.ASCII.GetString(buf, 0, len);
                }
            }
        }

        private static int ParseContentLength(string headers)
        {
            // minimal parser: Content-Length: N
            var lines = headers.Split(new[] { "\r\n" }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                int colon = line.IndexOf(':');
                if (colon <= 0) continue;

                var name = line.Substring(0, colon).Trim();
                if (!name.Equals("Content-Length", StringComparison.OrdinalIgnoreCase)) continue;

                var val = line.Substring(colon + 1).Trim();
                int n;
                if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                    return n;
            }
            return -1;
        }

        private static async Task<byte[]> ReadExactAsync(Stream s, int nBytes, CancellationToken ct)
        {
            if (nBytes == 0) return new byte[0];

            var buf = new byte[nBytes];
            int read = 0;

            while (read < nBytes)
            {
                ct.ThrowIfCancellationRequested();
                int n = await s.ReadAsync(buf, read, nBytes - read, ct).ConfigureAwait(false);
                if (n <= 0) throw new EndOfStreamException("EOF while reading payload");
                read += n;
            }

            return buf;
        }

        private static async Task<int> ReadByteAsync(Stream s, CancellationToken ct)
        {
            var b = new byte[1];
            int n = await s.ReadAsync(b, 0, 1, ct).ConfigureAwait(false);
            if (n <= 0) return -1;
            return b[0];
        }
    }
}
