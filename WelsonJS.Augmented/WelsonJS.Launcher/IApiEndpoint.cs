// IResourceTool.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System.Net;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Defines a contract for resource tools that can handle specific HTTP requests.
    /// </summary>
    public interface IApiEndpoint
    {
        /// <summary>
        /// Determines whether this tool can handle the specified path.
        /// </summary>
        /// <param name="path">The request path to check.</param>
        /// <returns>True if this tool can handle the request; otherwise, false.</returns>
        bool CanHandle(string path);
        /// <summary>
        /// Asynchronously processes the HTTP request for the specified path.
        /// </summary>
        /// <param name="context">The HTTP listener context containing request and response objects.</param>
        /// <param name="path">The request path to handle.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task HandleAsync(HttpListenerContext context, string path);
    }
}