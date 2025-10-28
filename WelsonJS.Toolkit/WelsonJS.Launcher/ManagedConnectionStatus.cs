// ManagedConnectionStatus.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
namespace WelsonJS.Launcher
{
    /// <summary>
    /// Represents the state of a managed connection for UI presentation.
    /// </summary>
    public sealed class ManagedConnectionStatus
    {
        public ManagedConnectionStatus(string connectionType, string key, string state, string description, bool isValid)
        {
            ConnectionType = connectionType ?? string.Empty;
            Key = key ?? string.Empty;
            State = state ?? string.Empty;
            Description = description ?? string.Empty;
            IsValid = isValid;
        }

        public string ConnectionType { get; }

        public string Key { get; }

        public string State { get; }

        public string Description { get; }

        public bool IsValid { get; }
    }
}
