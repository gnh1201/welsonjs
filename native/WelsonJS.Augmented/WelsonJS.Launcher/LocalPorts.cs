// LocalPorts.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System.Net;
using System.Net.Sockets;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Ports for the local servers the launcher starts (the llama.cpp router, the browser's
    /// remote debugging), chosen by the launcher when they start instead of fixed in the
    /// settings, so they do not collide with other software or another launcher.
    /// </summary>
    internal static class LocalPorts
    {
        /// <summary>The address the launcher's local servers listen on.</summary>
        public const string Host = "127.0.0.1";

        /// <summary>
        /// A TCP port that is free on the host now (the system picks it). The caller passes it
        /// to the server it starts right away.
        /// </summary>
        public static int FindAvailable(string host = Host)
        {
            var listener = new TcpListener(IPAddress.Parse(host), 0);
            listener.Start();
            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
