// ConnectionMonitorRegistry.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Keeps track of connection providers that should appear in the monitor UI.
    /// </summary>
    public static class ConnectionMonitorRegistry
    {
        private static readonly object _syncRoot = new object();
        private static readonly List<IManagedConnectionProvider> _providers = new List<IManagedConnectionProvider>();

        public static void RegisterProvider(IManagedConnectionProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            lock (_syncRoot)
            {
                if (!_providers.Contains(provider))
                {
                    _providers.Add(provider);
                }
            }
        }

        public static void UnregisterProvider(IManagedConnectionProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            lock (_syncRoot)
            {
                _providers.Remove(provider);
            }
        }

        public static IReadOnlyList<IManagedConnectionProvider> GetProviders()
        {
            lock (_syncRoot)
            {
                return _providers.ToArray();
            }
        }
    }
}
