// IApiEndpoint.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System.Net;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Defines a contract for API endpoints that can selectively handle incoming HTTP requests.
    /// </summary>
    public interface IApiEndpoint
    {
        /// <summary>
        /// Determines whether this endpoint can handle a request
        /// based solely on the request path.
        /// </summary>
        /// <remarks>
        /// This method is typically used for fast, lightweight routing decisions
        /// before inspecting headers, HTTP methods, or request bodies.
        /// Implementations should avoid side effects and expensive operations.
        /// </remarks>
        /// <param name="path">The normalized request path (e.g. "/api/status").</param>
        /// <returns>
        /// <c>true</c> if this endpoint is responsible for the given path;
        /// otherwise, <c>false</c>.
        /// </returns>
        bool CanHandle(string path);

        /// <summary>
        /// Determines whether this endpoint can handle the given HTTP request
        /// using the full <see cref="HttpListenerContext"/>.
        /// </summary>
        /// <remarks>
        /// This overload allows more advanced routing decisions based on
        /// HTTP method, headers, query parameters, authentication state,
        /// or other contextual information.
        /// </remarks>
        /// <param name="context">
        /// The HTTP listener context containing request and connection details.
        /// </param>
        /// <returns>
        /// <c>true</c> if this endpoint can process the request;
        /// otherwise, <c>false</c>.
        /// </returns>
        bool CanHandle(HttpListenerContext context);

        /// <summary>
        /// Asynchronously processes the HTTP request handled by this endpoint.
        /// </summary>
        /// <param name="context">
        /// The HTTP listener context containing request and response objects.
        /// </param>
        /// <param name="path">
        /// The request path associated with this handler.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous handling operation.
        /// </returns>
        Task HandleAsync(HttpListenerContext context, string path);
    }
}