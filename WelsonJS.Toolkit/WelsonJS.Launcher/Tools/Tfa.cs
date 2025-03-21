using System;
using System.Linq;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace WelsonJS.Launcher.Tools
{
    public class Tfa
    {
        private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

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
            var rand = new Random();
            var key = new char[16];
            for (int i = 0; i < 16; i++)
            {
                key[i] = Base32Chars[rand.Next(Base32Chars.Length)];
            }
            return string.Join(" ", Enumerable.Range(0, 4).Select(i => new string(key, i * 4, 4)));
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