// TelemetryIdentity.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace WelsonJS.Launcher.Telemetry
{
    public static class TelemetryIdentity
    {
        /// <summary>
        /// Collects multiple hardware/OS identity sources (BIOS UUID, OS SerialNumber, MachineName),
        /// joins them with ',' and computes SHA-256 hash, then returns the first 16 hex characters
        /// as a compact Distinct ID.
        /// </summary>
        public static string GetDistinctId()
        {
            var sources = new List<string>();

            string biosUuid = GetBiosUuid();
            if (!string.IsNullOrEmpty(biosUuid))
                sources.Add(biosUuid);

            string osUuid = GetOsUuid();
            if (!string.IsNullOrEmpty(osUuid))
                sources.Add(osUuid);

            string machineName = GetMachineName();
            if (!string.IsNullOrEmpty(machineName))
                sources.Add(machineName);

            string raw = string.Join(",", sources);
            string hash = ComputeSha256(raw);

            if (!string.IsNullOrEmpty(hash) && hash.Length >= 16)
                return hash.Substring(0, 16);

            return hash;
        }

        private static string GetMachineName()
        {
            try { return Environment.MachineName ?? ""; }
            catch { return ""; }
        }

        /// <summary>
        /// Retrieves BIOS UUID from Win32_ComputerSystemProduct.
        /// Filters out invalid values (all zeros or all 'F').
        /// </summary>
        private static string GetBiosUuid()
        {
            try
            {
                using (var searcher =
                    new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string uuid = obj["UUID"] as string;
                        if (string.IsNullOrEmpty(uuid))
                            return null;

                        uuid = uuid.Trim();
                        string compact = uuid.Replace("-", "").ToUpperInvariant();

                        // Exclude invalid dummy UUIDs such as all 0s or all Fs
                        if (IsAllChar(compact, '0') || IsAllChar(compact, 'F'))
                            return null;

                        return uuid;
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Retrieves OS-level UUID equivalent using Win32_OperatingSystem.SerialNumber.
        /// This value is unique per Windows installation.
        /// </summary>
        private static string GetOsUuid()
        {
            try
            {
                using (var searcher =
                    new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string serial = obj["SerialNumber"] as string;
                        if (!string.IsNullOrEmpty(serial))
                            return serial.Trim();
                    }
                }
            }
            catch { }

            return null;
        }

        private static bool IsAllChar(string s, char c)
        {
            if (string.IsNullOrEmpty(s)) return false;
            for (int i = 0; i < s.Length; i++)
                if (s[i] != c) return false;
            return true;
        }

        /// <summary>
        /// Computes SHA-256 hex string for the given input text.
        /// Returns null if hashing fails.
        /// </summary>
        private static string ComputeSha256(string input)
        {
            if (input == null) input = "";

            try
            {
                using (var sha = SHA256.Create())
                {
                    var data = Encoding.UTF8.GetBytes(input);
                    var digest = sha.ComputeHash(data);

                    var sb = new StringBuilder(digest.Length * 2);
                    for (int i = 0; i < digest.Length; i++)
                        sb.Append(digest[i].ToString("x2"));

                    return sb.ToString();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
