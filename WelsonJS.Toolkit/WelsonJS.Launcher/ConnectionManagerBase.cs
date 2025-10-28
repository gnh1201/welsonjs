// ConnectionManagerBase.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Provides a reusable pattern for keeping long-lived connections alive and
    /// recreating them transparently when the underlying connection becomes invalid.
    /// </summary>
    /// <typeparam name="TParameters">A descriptor used to create a unique key for each connection.</typeparam>
    /// <typeparam name="TConnection">The concrete connection type.</typeparam>
    public abstract class ConnectionManagerBase<TParameters, TConnection>
        where TConnection : class
    {
        private readonly ConcurrentDictionary<string, (TConnection Connection, TParameters Parameters)> _pool
            = new ConcurrentDictionary<string, (TConnection, TParameters)>();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _openLocks
            = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _opLocks
            = new ConcurrentDictionary<string, SemaphoreSlim>();

        /// <summary>
        /// Creates a unique cache key for the given connection parameters.
        /// </summary>
        protected abstract string CreateKey(TParameters parameters);

        /// <summary>
        /// Establishes a new connection using the provided parameters.
        /// </summary>
        protected abstract Task<TConnection> OpenConnectionAsync(TParameters parameters, CancellationToken token);

        /// <summary>
        /// Validates whether the existing connection is still usable.
        /// </summary>
        protected abstract bool IsConnectionValid(TConnection connection);

        /// <summary>
        /// Releases the resources associated with a connection instance.
        /// </summary>
        protected virtual void CloseConnection(TConnection connection)
        {
            if (connection is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Retrieves a cached connection or creates a new one if needed.
        /// </summary>
        protected async Task<TConnection> GetOrCreateAsync(TParameters parameters, CancellationToken token)
        {
            string key = CreateKey(parameters);

            if (_pool.TryGetValue(key, out var existing) && IsConnectionHealthy(existing.Connection))
            {
                return existing.Connection;
            }

            var gate = _openLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_pool.TryGetValue(key, out existing) && IsConnectionHealthy(existing.Connection))
                {
                    return existing.Connection;
                }

                if (existing.Connection != null && !IsConnectionHealthy(existing.Connection))
                {
                    RemoveInternal(key, existing.Connection);
                }

                var connection = await OpenConnectionAsync(parameters, token).ConfigureAwait(false);
                _pool[key] = (connection, parameters);
                return connection;
            }
            finally
            {
                gate.Release();
            }
        }

        /// <summary>
        /// Removes the connection associated with the provided parameters.
        /// </summary>
        public void Remove(TParameters parameters)
        {
            string key = CreateKey(parameters);
            if (_pool.TryRemove(key, out var entry))
            {
                CloseSafely(entry.Connection);
            }
        }

        /// <summary>
        /// Removes the connection associated with the provided cache key.
        /// </summary>
        protected bool TryRemoveByKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (_pool.TryRemove(key, out var entry))
            {
                CloseSafely(entry.Connection);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Provides a snapshot of the currently tracked connections.
        /// </summary>
        protected IReadOnlyList<ConnectionSnapshot> SnapshotConnections()
        {
            var entries = _pool.ToArray();
            var result = new ConnectionSnapshot[entries.Length];

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                var connection = entry.Value.Connection;
                bool isValid = IsConnectionHealthy(connection);

                result[i] = new ConnectionSnapshot(
                    entry.Key,
                    entry.Value.Parameters,
                    connection,
                    isValid);
            }

            return result;
        }

        /// <summary>
        /// Executes an action against the managed connection, retrying once if the first attempt fails.
        /// </summary>
        protected async Task<TResult> ExecuteWithRetryAsync<TResult>(
            TParameters parameters,
            Func<TConnection, CancellationToken, Task<TResult>> operation,
            int maxAttempts,
            CancellationToken token)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));

            Exception lastError = null;
            var key = CreateKey(parameters);
            var opLock = _opLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                await opLock.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    token.ThrowIfCancellationRequested();
                    var connection = await GetOrCreateAsync(parameters, token).ConfigureAwait(false);
                    return await operation(connection, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    Remove(parameters);
                    if (attempt == maxAttempts - 1)
                    {
                        throw;
                    }
                }
                finally
                {
                    opLock.Release();
                }
            }

            throw lastError ?? new InvalidOperationException("Unreachable state in ExecuteWithRetryAsync");
        }

        private bool IsConnectionHealthy(TConnection connection)
        {
            if (connection == null)
            {
                return false;
            }

            try
            {
                return IsConnectionValid(connection);
            }
            catch
            {
                return false;
            }
        }

        private void RemoveInternal(string key, TConnection connection)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _pool.TryRemove(key, out _);
            }

            if (connection != null)
            {
                CloseSafely(connection);
            }
        }

        private void CloseSafely(TConnection connection)
        {
            try
            {
                CloseConnection(connection);
            }
            catch
            {
                // Ignore dispose exceptions.
            }
        }

        /// <summary>
        /// Represents an immutable snapshot of a managed connection.
        /// </summary>
        protected readonly struct ConnectionSnapshot
        {
            public ConnectionSnapshot(string key, TParameters parameters, TConnection connection, bool isValid)
            {
                Key = key;
                Parameters = parameters;
                Connection = connection;
                IsValid = isValid;
            }

            public string Key { get; }

            public TParameters Parameters { get; }

            public TConnection Connection { get; }

            public bool IsValid { get; }
        }
    }
}
