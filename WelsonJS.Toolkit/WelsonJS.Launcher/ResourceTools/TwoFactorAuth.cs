// TwoFactorAuth.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.IO;

namespace WelsonJS.Launcher.ResourceTools
{
    public class TwoFactorAuth : IResourceTool
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private const string Prefix = "tfa/";
        private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        private static readonly int[] ValidKeyCharLengths = new[] { 16, 32 };

        public TwoFactorAuth(ResourceServer server, HttpClient httpClient)
        {
            Server = server;
            _httpClient = httpClient;
        }

        public bool CanHandle(string path)
        {
            return path.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            string endpoint = path.Substring(Prefix.Length);

            // GET /tfa/pubkey[?len=16|32]
            if (endpoint.Equals("pubkey", StringComparison.OrdinalIgnoreCase))
            {
                int length = 32;
                var q = context.Request?.QueryString?["len"];
                if (!string.IsNullOrEmpty(q) &&
                    int.TryParse(q, out var parsed) &&
                    Array.IndexOf(ValidKeyCharLengths, parsed) >= 0)
                {
                    length = parsed;
                }

                Server.ServeResource(context, GetPubKey(length), "text/plain", 200);
                return;
            }

            // POST /tfa/otp
            // Body: secret=BASE32KEY
            if (endpoint.Equals("otp", StringComparison.OrdinalIgnoreCase) &&
                context.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                string body;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    body = await reader.ReadToEndAsync();
                }

                var parsed = ParseFormEncoded(body);
                if (!parsed.TryGetValue("secret", out string secret) || string.IsNullOrWhiteSpace(secret))
                {
                    Server.ServeResource(context, "missing 'secret' parameter", "text/plain", 400);
                    return;
                }

                try
                {
                    int otp = GetOtp(secret.Trim());
                    string otp6 = otp.ToString("D6"); // always 6 digits
                    Server.ServeResource(context, otp6, "text/plain", 200);
                }
                catch (Exception ex)
                {
                    Server.ServeResource(context, $"invalid secret: {ex.Message}", "text/plain", 400);
                }
                return;
            }

            // Default: not found
            Server.ServeResource(context, "not found", "text/plain", 404);
        }

        /// <summary>
        /// Compute a 6-digit TOTP from a Base32 secret.
        /// </summary>
        public int GetOtp(string key, int period = 30)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Secret key is required.", nameof(key));

            string normalized = NormalizeBase32(key);
            byte[] binaryKey = DecodeBase32(normalized);
            if (binaryKey.Length == 0)
                throw new ArgumentException("Secret could not be decoded.", nameof(key));

            long timestep = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / period;
            byte[] msg = BitConverter.GetBytes(timestep);
            if (BitConverter.IsLittleEndian) Array.Reverse(msg);

            using (var hmac = new HMACSHA1(binaryKey))
            {
                byte[] hash = hmac.ComputeHash(msg);
                int offset = hash[hash.Length - 1] & 0x0F;

                int binCode = ((hash[offset] & 0x7F) << 24)
                            | ((hash[offset + 1] & 0xFF) << 16)
                            | ((hash[offset + 2] & 0xFF) << 8)
                            | (hash[offset + 3] & 0xFF);

                return binCode % 1_000_000; // fixed 6 digits
            }
        }

        public string GetPubKey(int charCount = 32)
        {
            if (Array.IndexOf(ValidKeyCharLengths, charCount) < 0)
                throw new ArgumentException("charCount must be 16 or 32.", nameof(charCount));

            int bytesLen = (charCount * 5) / 8;

            using (var rng = RandomNumberGenerator.Create())
            {
                var randomBytes = new byte[bytesLen];
                rng.GetBytes(randomBytes);

                string b32 = EncodeBase32(randomBytes);
                return b32.ToLowerInvariant();
            }
        }

        private static string NormalizeBase32(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (ch == ' ' || ch == '-') continue;
                sb.Append(char.ToUpperInvariant(ch));
            }
            return sb.ToString();
        }

        private static string EncodeBase32(byte[] data)
        {
            if (data == null || data.Length == 0) return string.Empty;

            var sb = new StringBuilder((data.Length * 8 + 4) / 5);
            int buffer = 0, bitsLeft = 0;

            foreach (byte b in data)
            {
                buffer = (buffer << 8) | b;
                bitsLeft += 8;
                while (bitsLeft >= 5)
                {
                    sb.Append(Base32Chars[(buffer >> (bitsLeft - 5)) & 31]);
                    bitsLeft -= 5;
                }
            }

            if (bitsLeft > 0)
                sb.Append(Base32Chars[(buffer << (5 - bitsLeft)) & 31]);

            return sb.ToString();
        }

        private static byte[] DecodeBase32(string key)
        {
            int buffer = 0, bitsLeft = 0;
            var bytes = new List<byte>(key.Length * 5 / 8);

            foreach (char raw in key)
            {
                char c = char.ToUpperInvariant(raw);
                if (c == ' ' || c == '-') continue;

                int v = Base32Chars.IndexOf(c);
                if (v < 0) continue;

                buffer = (buffer << 5) | v;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    bitsLeft -= 8;
                    bytes.Add((byte)((buffer >> bitsLeft) & 0xFF));
                }
            }
            return bytes.ToArray();
        }

        /// <summary>
        /// Parse application/x-www-form-urlencoded into a dictionary (UTF-8 assumed).
        /// </summary>
        private static Dictionary<string, string> ParseFormEncoded(string body)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(body)) return result;

            var pairs = body.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var kv = pair.Split(new[] { '=' }, 2);
                string key = Uri.UnescapeDataString(kv[0] ?? "");
                string value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
                result[key] = value;
            }
            return result;
        }
    }
}
