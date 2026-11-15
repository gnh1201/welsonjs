// IApiKeyEndpoint.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
namespace WelsonJS.Launcher
{
    /// <summary>
    /// Marks an API endpoint that other software on this machine may also use with the
    /// launcher's API key (ResourceServerApiKey, sent as "Authorization: Bearer ..."), in
    /// addition to the trusted local requests that every endpoint accepts (see
    /// ResourceServer.IsTrustedLocalRequest and ResourceServer.HasValidApiKey).
    ///
    /// Endpoints that run local processes on request (terminals, scripts) should not be
    /// marked unless a caller with the key is meant to do that.
    /// </summary>
    public interface IApiKeyEndpoint : IApiEndpoint
    {
    }
}
