using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WelsonJS.Launcher.Tools
{
    public class DnsQuery
    {
        private static readonly Random _random = new Random();
        private readonly string _dnsServer;
        private const int DnsPort = 53;
        private const int Timeout = 5000;

        public DnsQuery(string dnsServer = "8.8.8.8")
        {
            _dnsServer = dnsServer;
        }

        public List<string> QueryA(string domain) => QueryDns(domain, 1);
        public List<string> QueryNS(string domain) => QueryDns(domain, 2);
        public List<string> QueryCNAME(string domain) => QueryDns(domain, 5);
        public List<string> QuerySOA(string domain) => QueryDns(domain, 6);
        public List<string> QueryPTR(string domain) => QueryDns(domain, 12);
        public List<string> QueryMX(string domain) => QueryDns(domain, 15);
        public List<string> QueryTXT(string domain) => QueryDns(domain, 16);
        public List<string> QueryAAAA(string domain) => QueryDns(domain, 28);
        public List<string> QuerySRV(string domain) => QueryDns(domain, 33);
        public List<string> QueryNAPTR(string domain) => QueryDns(domain, 35);
        public List<string> QueryCAA(string domain) => QueryDns(domain, 257);

        public Dictionary<string, List<string>> QueryAll(string domain)
        {
            var results = new Dictionary<string, List<string>>();

            results["A"] = QueryA(domain);
            results["NS"] = QueryNS(domain);
            results["CNAME"] = QueryCNAME(domain);
            results["SOA"] = QuerySOA(domain);
            results["PTR"] = QueryPTR(domain);
            results["MX"] = QueryMX(domain);
            results["TXT"] = QueryTXT(domain);
            results["AAAA"] = QueryAAAA(domain);
            results["SRV"] = QuerySRV(domain);
            results["NAPTR"] = QueryNAPTR(domain);
            results["CAA"] = QueryCAA(domain);

            return results;
        }

        private List<string> QueryDns(string domain, ushort type)
        {
            List<string> records = new List<string>();

            try
            {
                UdpClient udpClient = new UdpClient(_dnsServer, DnsPort);
                udpClient.Client.ReceiveTimeout = Timeout;

                byte[] request = CreateDnsQuery(domain, type);
                udpClient.Send(request, request.Length);

                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, DnsPort);
                byte[] response = udpClient.Receive(ref remoteEP);

                records.AddRange(ParseDnsResponse(response, type));
                udpClient.Close();
            }
            catch (Exception ex)
            {
                records.Add($"Error: {ex.Message}");
            }

            return records;
        }

        private byte[] CreateDnsQuery(string domain, ushort type)
        {
            byte[] query = new byte[512];

            query[0] = (byte)_random.Next(0, 256);
            query[1] = (byte)_random.Next(0, 256);
            query[2] = 0x01;
            query[3] = 0x00;
            query[4] = 0x00;
            query[5] = 0x01;

            for (int i = 6; i < 12; i++)
                query[i] = 0x00;

            int position = 12;
            foreach (string part in domain.Split('.'))
            {
                query[position++] = (byte)part.Length;
                byte[] label = Encoding.ASCII.GetBytes(part);
                Array.Copy(label, 0, query, position, label.Length);
                position += label.Length;
            }
            query[position++] = 0x00;

            query[position++] = (byte)(type >> 8);
            query[position++] = (byte)(type & 0xFF);
            query[position++] = 0x00;
            query[position++] = 0x01;

            byte[] finalQuery = new byte[position];
            Array.Copy(query, finalQuery, position);

            return finalQuery;
        }

        private List<string> ParseDnsResponse(byte[] response, ushort queryType)
        {
            List<string> results = new List<string>();

            int answerCount = (response[6] << 8) | response[7];
            if (answerCount == 0)
            {
                results.Add("No records found.");
                return results;
            }

            int position = 12; // Skip DNS header

            while (response[position] != 0)
                position += response[position] + 1;
            position += 5; // End of Question section

            for (int i = 0; i < answerCount; i++)
            {
                while (response[position] != 0 && (response[position] & 0xC0) == 0)
                    position += response[position] + 1;
                position += 2; // Skip Type, Class fields

                ushort recordType = (ushort)((response[position] << 8) | response[position + 1]);
                position += 8; // Skip TTL and Data length fields

                ushort dataLength = (ushort)((response[position] << 8) | response[position + 1]);
                position += 2; // Read Data Length

                byte[] data = new byte[dataLength];
                Array.Copy(response, position, data, 0, dataLength);
                position += dataLength;

                switch (recordType)
                {
                    case 1:
                        results.Add($"A: {new IPAddress(data)}");
                        break;
                    case 2:
                        results.Add($"NS: {DecodeDomainName(response, data, 0)}");
                        break;
                    case 5:
                        results.Add($"CNAME: {DecodeDomainName(response, data, 0)}");
                        break;
                    case 6:
                        results.Add($"SOA: {DecodeDomainName(response, data, 0)}");
                        break;
                    case 12:
                        results.Add($"PTR: {DecodeDomainName(response, data, 0)}");
                        break;
                    case 15:
                        // MX record processing (priority and exchange)
                        if (data.Length >= 2)
                        {
                            ushort priority = (ushort)((data[0] << 8) | data[1]);
                            string exchange = DecodeDomainName(response, data, 2); // Decode domain name after 2 bytes
                            results.Add($"MX: Priority {priority}, Exchange {exchange}");
                        }
                        else
                        {
                            results.Add($"MX: Invalid data length.");
                        }
                        break;
                    case 16:
                        int txtPos = 0;
                        while (txtPos < data.Length)
                        {
                            int txtLength = data[txtPos++];
                            results.Add($"TXT: {Encoding.UTF8.GetString(data, txtPos, txtLength)}");
                            txtPos += txtLength;
                        }
                        break;
                    case 28:
                        results.Add($"AAAA: {new IPAddress(data)}");
                        break;
                    case 33:
                        ushort prioritySrv = (ushort)((data[0] << 8) | data[1]);
                        ushort weight = (ushort)((data[2] << 8) | data[3]);
                        ushort port = (ushort)((data[4] << 8) | data[5]);
                        string target = DecodeDomainName(response, data, 6);
                        results.Add($"SRV: Priority {prioritySrv}, Weight {weight}, Port {port}, Target {target}");
                        break;
                    case 35:
                        results.Add($"NAPTR: {BitConverter.ToString(data)}");
                        break;
                    case 257:
                        results.Add($"CAA: {BitConverter.ToString(data)}");
                        break;
                    default:
                        results.Add($"Unknown Type {recordType}: {BitConverter.ToString(data)}");
                        break;
                }
            }

            return results;
        }

        private string DecodeDomainName(byte[] response, byte[] data, int startIndex)
        {
            int position = startIndex;
            List<string> labels = new List<string>();

            while (data[position] != 0)
            {
                // Handle 0xC0 pointer (compressed domain name handling)
                if ((data[position] & 0xC0) == 0xC0)
                {
                    int pointer = ((data[position] & 0x3F) << 8) | data[position + 1];
                    return DecodeDomainName(response, response, pointer); // Recursive call to decode from the pointer
                }

                int labelLength = data[position++];
                labels.Add(Encoding.ASCII.GetString(data, position, labelLength));
                position += labelLength;
            }

            return string.Join(".", labels);
        }
    }
}
