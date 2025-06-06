﻿// Tfa.cs
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

namespace WelsonJS.Launcher.ResourceTools
{
    public class Tfa : IResourceTool
    {
        private readonly ResourceServer Server;
        private readonly HttpClient _httpClient;
        private const string Prefix = "tfa/";
        private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        public Tfa(ResourceServer server, HttpClient httpClient)
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
            await Task.Delay(0);

            string endpoint = path.Substring(Prefix.Length);

            if (endpoint.Equals("pubkey"))
            {
                Server.ServeResource(context, GetPubKey(), "text/plain", 200);
                return;
            }

            Server.ServeResource(context);
        }

        public int GetOtp(string key)
        {
            byte[] binaryKey = DecodeBase32(key.Replace(" ", ""));
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
            byte[] timestampBytes = BitConverter.GetBytes(timestamp);
            Array.Reverse(timestampBytes); // Ensure big-endian order

            using (var hmac = new HMACSHA1(binaryKey))
            {
                byte[] hash = hmac.ComputeHash(timestampBytes);
                int offset = hash[hash.Length - 1] & 0xF;

                int otp = ((hash[offset] & 0x7F) << 24) |
                          ((hash[offset + 1] & 0xFF) << 16) |
                          ((hash[offset + 2] & 0xFF) << 8) |
                          (hash[offset + 3] & 0xFF);

                return otp % 1000000; // Ensure 6-digit OTP
            }
        }

        public string GetPubKey()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var key = new char[16];
                var randomBytes = new byte[16];
                rng.GetBytes(randomBytes);

                for (int i = 0; i < 16; i++)
                {
                    key[i] = Base32Chars[randomBytes[i] % Base32Chars.Length];
                }

                return string.Join(" ", Enumerable.Range(0, 4).Select(i => new string(key, i * 4, 4)));
            }
        }

        private static byte[] DecodeBase32(string key)
        {
            int buffer = 0, bitsLeft = 0;
            var binaryKey = new List<byte>();

            foreach (char c in key)
            {
                int value = Base32Chars.IndexOf(c);
                if (value < 0) continue; // Ignore invalid characters

                buffer = (buffer << 5) + value;
                bitsLeft += 5;
                if (bitsLeft >= 8)
                {
                    bitsLeft -= 8;
                    binaryKey.Add((byte)((buffer >> bitsLeft) & 0xFF));
                }
            }
            return binaryKey.ToArray();
        }
    }
}