// LocalServers.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace WelsonJS.Launcher.ApiEndpoints
{
    /// <summary>
    /// The local servers the launcher runs (LocalServerRegistry), in one report:
    ///
    ///   GET localservers/status   each server's process state: running, pid, port, base URL,
    ///                             last use, requests in progress, last exit code and the
    ///                             last lines it wrote
    ///
    /// Starting and stopping stay with each server's own API (llamacpp/*, crispembed/*).
    /// The report holds local paths and process output, so only local callers may read it.
    /// </summary>
    public sealed class LocalServers : IApiEndpoint
    {
        private const string StatusPath = "localservers/status";

        private readonly LocalServerRegistry _registry;

        public LocalServers(ResourceServer server, System.Net.Http.HttpClient httpClient, ILogger logger)
        {
            _registry = server.LocalServers;
        }

        public bool CanHandle(HttpListenerContext context, string path)
        {
            return context != null && path != null &&
                string.Equals(path, StatusPath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(context.Request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase);
        }

        public async Task HandleAsync(HttpListenerContext context, string path)
        {
            if (!ResourceServer.IsTrustedLocalRequest(context))
            {
                await LlamaCpp.WriteRaw(context, JsonCodec.Serialize(new { error = "The local server status is only available to local callers." }), 403);
                return;
            }
            await LlamaCpp.WriteRaw(context, JsonCodec.Serialize(new Dictionary<string, object> { ["servers"] = _registry.Status() }), 200);
        }
    }
}
