﻿// DnsQuery.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
// GET /dns-query/{name}[?type=MX,TXT]
//
// Asks the DNS server (DnsServerAddress: one address, or several separated by commas, the
// later ones as fallbacks) for the name's records: every type below, or the types given in
// ?type=. A server that does not answer, fails or refuses is followed by the next one; every
// server is asked once before any is asked again (Rounds). A name that does not exist
// (NXDOMAIN) is an answer, not a failure. An IP address is looked up in reverse (PTR of in-addr.arpa / ip6.arpa).
// Internationalized names are converted to punycode. Each query goes over UDP, again over
// TCP when the answer is truncated, and the queries run at the same time.
//
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ApiEndpoints
{
    public class DnsQuery : IApiEndpoint
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private const string Prefix = "dns-query/";
        private const int DnsPort = 53;
        private const int TimeoutMs = 3000;       // per attempt
        private const int Rounds = 2;             // times every server is tried
        private const string DefaultServers = "1.1.1.1,8.8.8.8";

        // The record types asked for by default, in output order.
        private static readonly KeyValuePair<string, ushort>[] RecordTypes =
        {
            new KeyValuePair<string, ushort>("A", 1),
            new KeyValuePair<string, ushort>("AAAA", 28),
            new KeyValuePair<string, ushort>("CNAME", 5),
            new KeyValuePair<string, ushort>("NS", 2),
            new KeyValuePair<string, ushort>("SOA", 6),
            new KeyValuePair<string, ushort>("MX", 15),
            new KeyValuePair<string, ushort>("TXT", 16),
            new KeyValuePair<string, ushort>("SRV", 33),
            new KeyValuePair<string, ushort>("NAPTR", 35),
            new KeyValuePair<string, ushort>("CAA", 257),
            new KeyValuePair<string, ushort>("PTR", 12)
        };

        private static readonly Regex LabelPattern = new Regex(@"^[A-Za-z0-9_](?:[A-Za-z0-9_-]{0,61}[A-Za-z0-9_])?$");
        private static readonly IdnMapping Idn = new IdnMapping();

        private readonly IPAddress[] _servers;

        public DnsQuery(ResourceServer server, HttpClient httpClient, ILogger logger)
        {
            Server = server;

            _httpClient = httpClient;
            _logger = logger;

            _servers = ParseServers(Program.GetAppConfig("DnsServerAddress"));
        }

        public bool CanHandle(HttpListenerContext context, string path)
        {
            return path != null && path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            string input = Uri.UnescapeDataString(path.Substring(Prefix.Length)).Trim();

            string name;
            bool reverse;
            string error = NormalizeName(input, out name, out reverse);
            if (error != null)
            {
                await Server.ServeResource(context, "<error>" + WebUtility.HtmlEncode(error) + "</error>", "application/xml", 400);
                return;
            }

            KeyValuePair<string, ushort>[] types;
            if (reverse)
            {
                types = RecordTypes.Where(t => t.Key == "PTR").ToArray();
            }
            else
            {
                error = ParseTypes(context.Request.QueryString["type"], out types);
                if (error != null)
                {
                    await Server.ServeResource(context, "<error>" + WebUtility.HtmlEncode(error) + "</error>", "application/xml", 400);
                    return;
                }
            }

            try
            {
                QueryResult[] results = await Task.WhenAll(types.Select(t => QueryAsync(name, t.Key, t.Value)));
                await Server.ServeResource(context, Format(input, name, results), "text/plain", 200);
            }
            catch (Exception ex)
            {
                _logger.Error("Error processing DNS query: " + ex.Message);
                await Server.ServeResource(context, "<error>Failed to process DNS query. " + WebUtility.HtmlEncode(ex.Message) + "</error>", "application/xml", 500);
            }
        }

        // ---------------------------------------------------------------- input

        private static IPAddress[] ParseServers(string setting)
        {
            var servers = new List<IPAddress>();
            foreach (string part in (setting ?? "").Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (IPAddress.TryParse(part.Trim(), out IPAddress address))
                    servers.Add(address);
            }
            return servers.Count > 0 ? servers.Distinct().ToArray() : ParseServers(DefaultServers);
        }

        // Turns the input into the name to ask for: an IP address into its reverse name, an
        // internationalized name into punycode. Returns an error message, or null.
        private static string NormalizeName(string input, out string name, out bool reverse)
        {
            name = null;
            reverse = false;
            if (string.IsNullOrEmpty(input) || input.Length > 255)
                return "Enter a hostname or IP address (up to 255 characters).";

            if (IPAddress.TryParse(input, out IPAddress address))
            {
                name = ReverseName(address);
                reverse = true;
                return null;
            }

            string ascii;
            try
            {
                ascii = Idn.GetAscii(input.TrimEnd('.'));
            }
            catch (ArgumentException)
            {
                return "Invalid hostname: " + input;
            }

            if (ascii.Length > 253 || !ascii.Split('.').All(label => LabelPattern.IsMatch(label)))
                return "Invalid hostname: " + input;

            name = ascii.ToLowerInvariant();
            return null;
        }

        private static string ReverseName(IPAddress address)
        {
            byte[] bytes = address.GetAddressBytes();
            if (address.AddressFamily == AddressFamily.InterNetwork)
                return string.Join(".", bytes.Reverse().Select(b => b.ToString(CultureInfo.InvariantCulture))) + ".in-addr.arpa";

            var nibbles = new List<string>();
            foreach (byte b in bytes.Reverse())
            {
                nibbles.Add((b & 0x0F).ToString("x"));
                nibbles.Add((b >> 4).ToString("x"));
            }
            return string.Join(".", nibbles) + ".ip6.arpa";
        }

        private static string ParseTypes(string value, out KeyValuePair<string, ushort>[] types)
        {
            types = RecordTypes;
            if (string.IsNullOrWhiteSpace(value)) return null;

            var selected = new List<KeyValuePair<string, ushort>>();
            foreach (string part in value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string key = part.Trim().ToUpperInvariant();
                KeyValuePair<string, ushort> type = RecordTypes.FirstOrDefault(t => t.Key == key);
                if (type.Key == null)
                    return "Unsupported record type: " + part.Trim() + " (supported: " + string.Join(", ", RecordTypes.Select(t => t.Key)) + ")";
                if (!selected.Contains(type)) selected.Add(type);
            }
            types = selected.ToArray();
            return null;
        }

        // ---------------------------------------------------------------- query

        private sealed class DnsRecord
        {
            public string Type;
            public uint Ttl;
            public string Data;
        }

        private sealed class QueryResult
        {
            public string TypeName;
            public IPAddress Server;
            public bool OverTcp;
            public List<DnsRecord> Records = new List<DnsRecord>();
            public string Error;      // the query failed (no answer, server error)
            public bool NoSuchName;   // NXDOMAIN
        }

        private async Task<QueryResult> QueryAsync(string name, string typeName, ushort type)
        {
            var result = new QueryResult { TypeName = typeName };
            // The last failure of each server; a server that failed other than by not
            // answering in time is not asked again.
            var errors = new Dictionary<IPAddress, string>();
            var skipped = new HashSet<IPAddress>();

            for (int round = 0; round < Rounds; round++)
            {
                foreach (IPAddress server in _servers)
                {
                    if (skipped.Contains(server)) continue;
                    result.OverTcp = false;
                    try
                    {
                        ushort id = NewId();
                        byte[] request = BuildQuery(id, name, type);
                        byte[] response = await SendUdpAsync(server, request);
                        CheckResponse(response, id);
                        if ((response[2] & 0x02) != 0) // TC: truncated, ask again over TCP
                        {
                            response = await SendTcpAsync(server, request);
                            CheckResponse(response, id);
                            result.OverTcp = true;
                        }

                        int rcode = response[3] & 0x0F;
                        if (rcode == 3)
                        {
                            result.Server = server;
                            result.NoSuchName = true;
                            return result;
                        }
                        if (rcode != 0)
                        {
                            // A server failure or refusal: the next server may answer.
                            errors[server] = ResponseCodeName(rcode);
                            skipped.Add(server);
                            continue;
                        }

                        result.Server = server;
                        result.Records = ParseAnswers(response);
                        return result;
                    }
                    catch (TimeoutException)
                    {
                        errors[server] = "no answer within " + (TimeoutMs / 1000) + " s";
                    }
                    catch (Exception ex) when (ex is SocketException || ex is IOException || ex is InvalidDataException || ex is ObjectDisposedException)
                    {
                        errors[server] = ex.Message;
                        skipped.Add(server);
                    }
                }
                if (skipped.Count == _servers.Length) break;
            }

            result.Error = string.Join("; ", errors.Select(e => e.Key + ": " + e.Value));
            return result;
        }

        private static ushort NewId()
        {
            byte[] bytes = new byte[2];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);
            return (ushort)((bytes[0] << 8) | bytes[1]);
        }

        private static byte[] BuildQuery(ushort id, string name, ushort type)
        {
            var query = new List<byte>
            {
                (byte)(id >> 8), (byte)id,
                0x01, 0x00,       // standard query, recursion desired
                0x00, 0x01,       // one question
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            foreach (string label in name.Split('.'))
            {
                query.Add((byte)label.Length);
                query.AddRange(Encoding.ASCII.GetBytes(label));
            }
            query.Add(0x00);
            query.Add((byte)(type >> 8));
            query.Add((byte)type);
            query.Add(0x00);
            query.Add(0x01);      // class IN
            return query.ToArray();
        }

        private static async Task<byte[]> SendUdpAsync(IPAddress server, byte[] request)
        {
            using (var udp = new UdpClient(server.AddressFamily))
            {
                udp.Connect(server, DnsPort);
                await udp.SendAsync(request, request.Length);
                Task<UdpReceiveResult> receive = udp.ReceiveAsync();
                if (await Task.WhenAny(receive, Task.Delay(TimeoutMs)) != receive)
                {
                    // Closing the client ends the receive with an error, which is observed here.
                    _ = receive.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                    throw new TimeoutException();
                }
                return receive.Result.Buffer;
            }
        }

        private static async Task<byte[]> SendTcpAsync(IPAddress server, byte[] request)
        {
            using (var tcp = new TcpClient(server.AddressFamily))
            {
                Task connect = tcp.ConnectAsync(server, DnsPort);
                if (await Task.WhenAny(connect, Task.Delay(TimeoutMs)) != connect)
                    throw new TimeoutException();
                await connect;

                NetworkStream stream = tcp.GetStream();
                byte[] framed = new byte[request.Length + 2];
                framed[0] = (byte)(request.Length >> 8);
                framed[1] = (byte)request.Length;
                Array.Copy(request, 0, framed, 2, request.Length);
                await stream.WriteAsync(framed, 0, framed.Length);

                byte[] length = await ReadExactlyAsync(stream, 2);
                return await ReadExactlyAsync(stream, (length[0] << 8) | length[1]);
            }
        }

        private static async Task<byte[]> ReadExactlyAsync(NetworkStream stream, int count)
        {
            byte[] buffer = new byte[count];
            int read = 0;
            while (read < count)
            {
                Task<int> chunk = stream.ReadAsync(buffer, read, count - read);
                if (await Task.WhenAny(chunk, Task.Delay(TimeoutMs)) != chunk)
                {
                    _ = chunk.ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted);
                    throw new TimeoutException();
                }
                int n = await chunk;
                if (n == 0) throw new IOException("The DNS server closed the connection.");
                read += n;
            }
            return buffer;
        }

        private static void CheckResponse(byte[] response, ushort id)
        {
            if (response.Length < 12)
                throw new InvalidDataException("The DNS answer is too short.");
            if (((response[0] << 8) | response[1]) != id || (response[2] & 0x80) == 0)
                throw new InvalidDataException("The DNS answer does not match the query.");
        }

        private static string ResponseCodeName(int rcode)
        {
            switch (rcode)
            {
                case 1: return "Format error";
                case 2: return "Server failure";
                case 3: return "Name does not exist";
                case 4: return "Not implemented";
                case 5: return "Refused";
                default: return "Error " + rcode;
            }
        }

        // ---------------------------------------------------------------- parsing

        private static List<DnsRecord> ParseAnswers(byte[] msg)
        {
            var records = new List<DnsRecord>();
            int questions = ReadUInt16(msg, 4);
            int answers = ReadUInt16(msg, 6);
            int pos = 12;

            for (int i = 0; i < questions; i++)
            {
                ReadName(msg, ref pos);
                pos += 4; // type, class
            }

            for (int i = 0; i < answers; i++)
            {
                ReadName(msg, ref pos);
                ushort type = ReadUInt16(msg, pos);
                uint ttl = ReadUInt32(msg, pos + 4);
                int length = ReadUInt16(msg, pos + 8);
                pos += 10;
                if (pos + length > msg.Length)
                    throw new InvalidDataException("A DNS record is longer than the answer.");

                records.Add(new DnsRecord { Type = TypeName(type), Ttl = ttl, Data = FormatData(msg, type, pos, length) });
                pos += length;
            }
            return records;
        }

        private static string TypeName(ushort type)
        {
            foreach (KeyValuePair<string, ushort> t in RecordTypes)
                if (t.Value == type) return t.Key;
            return "TYPE" + type;
        }

        private static string FormatData(byte[] msg, ushort type, int start, int length)
        {
            int pos = start;
            int end = start + length;
            switch (type)
            {
                case 1:  // A
                case 28: // AAAA
                    if (length != (type == 1 ? 4 : 16)) break;
                    byte[] address = new byte[length];
                    Array.Copy(msg, start, address, 0, length);
                    return new IPAddress(address).ToString();

                case 2:  // NS
                case 5:  // CNAME
                case 12: // PTR
                    return ReadName(msg, ref pos);

                case 6:  // SOA
                {
                    string primary = ReadName(msg, ref pos);
                    string mailbox = ReadName(msg, ref pos);
                    if (pos + 20 > end) break;
                    return string.Format(CultureInfo.InvariantCulture,
                        "Primary {0}, Mailbox {1}, Serial {2}, Refresh {3}, Retry {4}, Expire {5}, Minimum TTL {6}",
                        primary, MailboxToAddress(mailbox), ReadUInt32(msg, pos), ReadUInt32(msg, pos + 4),
                        ReadUInt32(msg, pos + 8), ReadUInt32(msg, pos + 12), ReadUInt32(msg, pos + 16));
                }

                case 15: // MX
                {
                    if (length < 3) break;
                    ushort preference = ReadUInt16(msg, pos);
                    pos += 2;
                    return "Preference " + preference + ", Exchange " + ReadName(msg, ref pos);
                }

                case 16: // TXT: one record may hold several strings, which form one text
                {
                    var text = new StringBuilder();
                    while (pos < end)
                        text.Append(ReadCharacterString(msg, ref pos, end));
                    return "\"" + text + "\"";
                }

                case 33: // SRV
                {
                    if (length < 7) break;
                    ushort priority = ReadUInt16(msg, pos), weight = ReadUInt16(msg, pos + 2), port = ReadUInt16(msg, pos + 4);
                    pos += 6;
                    return "Priority " + priority + ", Weight " + weight + ", Port " + port + ", Target " + ReadName(msg, ref pos);
                }

                case 35: // NAPTR
                {
                    if (length < 7) break;
                    ushort order = ReadUInt16(msg, pos), preference = ReadUInt16(msg, pos + 2);
                    pos += 4;
                    string flags = ReadCharacterString(msg, ref pos, end);
                    string services = ReadCharacterString(msg, ref pos, end);
                    string regexp = ReadCharacterString(msg, ref pos, end);
                    string replacement = ReadName(msg, ref pos);
                    return "Order " + order + ", Preference " + preference + ", Flags \"" + flags + "\", Services \"" + services +
                        "\", Regexp \"" + regexp + "\", Replacement " + replacement;
                }

                case 257: // CAA
                {
                    if (length < 2) break;
                    byte flags = msg[pos];
                    int tagLength = msg[pos + 1];
                    if (2 + tagLength > length) break;
                    string tag = Encoding.ASCII.GetString(msg, pos + 2, tagLength);
                    string value = Encoding.UTF8.GetString(msg, pos + 2 + tagLength, length - 2 - tagLength);
                    return "Flags " + flags + ", Tag " + tag + ", Value \"" + value + "\"";
                }
            }
            return BitConverter.ToString(msg, start, length);
        }

        // An SOA mailbox (hostmaster.example.com) as the address it stands for.
        private static string MailboxToAddress(string mailbox)
        {
            int dot = mailbox.IndexOf('.');
            return dot > 0 ? mailbox.Substring(0, dot) + "@" + mailbox.Substring(dot + 1) : mailbox;
        }

        // Reads a domain name at pos (following compression pointers) and moves pos past it.
        private static string ReadName(byte[] msg, ref int pos)
        {
            var labels = new List<string>();
            int cursor = pos;
            int jumps = 0;
            bool jumped = false;

            while (true)
            {
                if (cursor >= msg.Length)
                    throw new InvalidDataException("A DNS name runs past the answer.");
                int length = msg[cursor];

                if ((length & 0xC0) == 0xC0)
                {
                    if (cursor + 1 >= msg.Length || ++jumps > 16)
                        throw new InvalidDataException("A DNS name has bad compression.");
                    if (!jumped) pos = cursor + 2;
                    cursor = ((length & 0x3F) << 8) | msg[cursor + 1];
                    jumped = true;
                    continue;
                }
                if (length == 0)
                {
                    if (!jumped) pos = cursor + 1;
                    break;
                }
                if (cursor + 1 + length > msg.Length)
                    throw new InvalidDataException("A DNS name runs past the answer.");
                labels.Add(Encoding.ASCII.GetString(msg, cursor + 1, length));
                cursor += 1 + length;
            }

            string name = string.Join(".", labels);
            return name.Length == 0 ? "." : ToUnicode(name);
        }

        // Shows punycode labels (xn--) in their own script as well.
        private static string ToUnicode(string name)
        {
            if (name.IndexOf("xn--", StringComparison.OrdinalIgnoreCase) < 0) return name;
            try
            {
                string unicode = Idn.GetUnicode(name);
                return unicode == name ? name : name + " (" + unicode + ")";
            }
            catch (ArgumentException)
            {
                return name;
            }
        }

        private static string ReadCharacterString(byte[] msg, ref int pos, int end)
        {
            if (pos >= end) return "";
            int length = msg[pos];
            if (pos + 1 + length > end)
                throw new InvalidDataException("A DNS text runs past its record.");
            string text = Encoding.UTF8.GetString(msg, pos + 1, length);
            pos += 1 + length;
            return text;
        }

        private static ushort ReadUInt16(byte[] msg, int pos)
        {
            if (pos + 2 > msg.Length) throw new InvalidDataException("The DNS answer is too short.");
            return (ushort)((msg[pos] << 8) | msg[pos + 1]);
        }

        private static uint ReadUInt32(byte[] msg, int pos)
        {
            if (pos + 4 > msg.Length) throw new InvalidDataException("The DNS answer is too short.");
            return ((uint)msg[pos] << 24) | ((uint)msg[pos + 1] << 16) | ((uint)msg[pos + 2] << 8) | msg[pos + 3];
        }

        // ---------------------------------------------------------------- output

        private static string Format(string input, string name, QueryResult[] results)
        {
            var text = new StringBuilder();
            text.AppendLine("Query: " + name + (name != input ? " (" + input + ")" : ""));
            IPAddress server = results.Select(r => r.Server).FirstOrDefault(s => s != null);
            if (server != null) text.AppendLine("Server: " + server);

            if (results.All(r => r.NoSuchName))
            {
                text.AppendLine();
                text.AppendLine("The name does not exist (NXDOMAIN).");
                return text.ToString();
            }

            var empty = new List<string>();
            foreach (QueryResult result in results)
            {
                if (result.Error == null && !result.NoSuchName && result.Records.Count == 0)
                {
                    empty.Add(result.TypeName);
                    continue;
                }

                text.AppendLine();
                text.AppendLine(result.TypeName + " Records:" + (result.OverTcp ? " (over TCP)" : ""));
                if (result.Error != null)
                    text.AppendLine("Error: " + result.Error);
                else if (result.NoSuchName)
                    text.AppendLine("The name does not exist (NXDOMAIN).");
                foreach (DnsRecord record in result.Records)
                    text.AppendLine(record.Type + ": " + record.Data + " (TTL " + record.Ttl + ")");
            }

            if (empty.Count > 0)
            {
                text.AppendLine();
                text.AppendLine("No records: " + string.Join(", ", empty));
            }
            return text.ToString();
        }
    }
}
