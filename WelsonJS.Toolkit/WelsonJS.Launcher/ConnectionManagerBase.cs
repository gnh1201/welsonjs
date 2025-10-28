// ConnectionManagerBase.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<string, TConnection> _pool = new ConcurrentDictionary<string, TConnection>();

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

            if (_pool.TryGetValue(key, out var existing) && IsConnectionValid(existing))
            {
                return existing;
            }

            RemoveInternal(key, existing);

            var connection = await OpenConnectionAsync(parameters, token).ConfigureAwait(false);
            _pool[key] = connection;
            return connection;
        }

        /// <summary>
        /// Removes the connection associated with the provided parameters.
        /// </summary>
        public void Remove(TParameters parameters)
        {
            string key = CreateKey(parameters);
            if (_pool.TryRemove(key, out var connection))
            {
                CloseSafely(connection);
            }
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

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                token.ThrowIfCancellationRequested();
                var connection = await GetOrCreateAsync(parameters, token).ConfigureAwait(false);

                try
                {
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
            }

            throw lastError ?? new InvalidOperationException("Unreachable state in ExecuteWithRetryAsync");
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
    }
}
