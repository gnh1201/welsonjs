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
        /// Determines whether this endpoint is able to handle the given HTTP request.
        /// </summary>
        /// <remarks>
        /// This method is invoked by the request dispatcher to decide which endpoint
        /// should process the incoming request.
        ///
        /// Implementations may use the provided <paramref name="path"/> for fast
        /// routing decisions, while the full <paramref name="context"/> can be
        /// inspected for HTTP method, headers, query parameters, authentication
        /// state, or other request-specific information.
        ///
        /// This method should be free of side effects and must not write to the response.
        /// </remarks>
        /// <param name="context">
        /// The HTTP listener context containing the incoming request details.
        /// </param>
        /// <param name="path">
        /// The normalized request path extracted from the request URL
        /// (e.g. "/api/status").
        /// </param>
        /// <returns>
        /// <c>true</c> if this endpoint is responsible for handling the request;
        /// otherwise, <c>false</c>.
        /// </returns>
        bool CanHandle(HttpListenerContext context, string path);

        /// <summary>
        /// Asynchronously handles the HTTP request assigned to this endpoint.
        /// </summary>
        /// <remarks>
        /// This method is called only after <see cref="CanHandle"/> has returned
        /// <c>true</c> for the same request.
        ///
        /// Implementations are responsible for writing the response and properly
        /// terminating the request lifecycle.
        /// </remarks>
        /// <param name="context">
        /// The HTTP listener context containing request and response objects.
        /// </param>
        /// <param name="path">
        /// The request path associated with this endpoint.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous handling operation.
        /// </returns>
        Task HandleAsync(HttpListenerContext context, string path);
    }
}