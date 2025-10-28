// IManagedConnectionProvider.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System.Collections.Generic;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Exposes connection status information for use by the connection monitor UI.
    /// </summary>
    public interface IManagedConnectionProvider
    {
        /// <summary>
        /// Gets a human-friendly name for the connection type managed by this provider.
        /// </summary>
        string ConnectionType { get; }

        /// <summary>
        /// Retrieves the current connections handled by the provider.
        /// </summary>
        IReadOnlyCollection<ManagedConnectionStatus> GetStatuses();

        /// <summary>
        /// Attempts to close the connection associated with the supplied cache key.
        /// </summary>
        bool TryClose(string key);
    }
}
