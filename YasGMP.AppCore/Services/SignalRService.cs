using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using YasGMP.Models.DTO;

namespace YasGMP.Services
{
    /// <summary>
    /// Real-time SignalR client for receiving new audit events instantly.
    /// </summary>
    public static class SignalRService
    {
        /// <summary>Back-channel connection; initialized in <see cref="InitializeAsync"/>.</summary>
        private static HubConnection? _hub; // CS8618 fix: nullable
        private static readonly SemaphoreSlim Sync = new(1, 1);

        /// <summary>Raised when a server-side AuditCreated event is received.</summary>
        public static event Func<AuditEntryDto, Task>? OnAuditReceived; // CS8618 fix: nullable

        /// <summary>Raised when the hub connection transitions into reconnecting state.</summary>
        public static event Func<Exception?, Task>? OnReconnecting;

        /// <summary>Raised when the hub connection has reconnected.</summary>
        public static event Func<string?, Task>? OnReconnected;

        /// <summary>Raised when the hub connection is closed and will no longer retry.</summary>
        public static event Func<Exception?, Task>? OnConnectionClosed;

        /// <summary>Creates and starts a SignalR connection to the specified hub URL.</summary>
        public static async Task InitializeAsync(string hubUrl)
        {
            if (string.IsNullOrWhiteSpace(hubUrl))
                throw new ArgumentException("Hub URL must be provided.", nameof(hubUrl));

            await Sync.WaitAsync().ConfigureAwait(false);
            HubConnection? previous = null;
            try
            {
                if (_hub != null)
                {
                    if (_hub.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
                    {
                        return;
                    }

                    previous = _hub;
                    _hub = null;
                }

                var connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect()
                    .Build();

                connection.On<AuditEntryDto>("AuditCreated", async audit =>
                {
                    if (OnAuditReceived != null)
                    {
                        await InvokeAsync(OnAuditReceived, audit).ConfigureAwait(false);
                    }
                });

                connection.Closed += ex => InvokeAsync(OnConnectionClosed, ex);
                connection.Reconnecting += ex => InvokeAsync(OnReconnecting, ex);
                connection.Reconnected += id => InvokeAsync(OnReconnected, id);

                _hub = connection;
                await connection.StartAsync().ConfigureAwait(false);
            }
            finally
            {
                Sync.Release();

                if (previous != null)
                {
                    try
                    {
                        await previous.DisposeAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        // Swallow disposal failures; callers rely on retry loop to re-create connections.
                    }
                }
            }
        }

        private static async Task InvokeAsync<T>(Func<T, Task> handler, T argument)
        {
            foreach (Func<T, Task> subscriber in handler.GetInvocationList())
            {
                await subscriber(argument).ConfigureAwait(false);
            }
        }

        private static Task InvokeAsync(Func<Exception?, Task>? handler, Exception? argument)
        {
            if (handler is null)
                return Task.CompletedTask;

            return InvokeAsyncInternal(handler, argument);
        }

        private static Task InvokeAsync(Func<string?, Task>? handler, string? argument)
        {
            if (handler is null)
                return Task.CompletedTask;

            return InvokeAsyncInternal(handler, argument);
        }

        private static async Task InvokeAsyncInternal<T>(Func<T, Task> handler, T argument)
        {
            foreach (Func<T, Task> subscriber in handler.GetInvocationList())
            {
                await subscriber(argument).ConfigureAwait(false);
            }
        }
    }
}
