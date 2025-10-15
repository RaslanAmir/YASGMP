using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using YasGMP.Models.DTO;
using YasGMP.Services;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Desktop-specific wrapper around <see cref="SignalRService"/> that marshals
/// live updates through WPF's dispatcher and keeps the shared <see cref="BackgroundScheduler"/>
/// alive for preventative maintenance jobs.
/// </summary>
public sealed class SignalRClientService : ISignalRClientService, IDisposable
{
    private readonly IUiDispatcher _dispatcher;
    private readonly TimeProvider _timeProvider;
    private readonly Lazy<BackgroundScheduler> _schedulerFactory;
    private readonly ISignalRClientAdapter _adapter;
    private readonly IReadOnlyList<TimeSpan> _retrySchedule;
    private readonly string _hubUrl;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _gate = new();
    private Task? _runTask;
    private int _attempt;
    private bool _disposed;

    /// <summary>Initializes a new instance of the <see cref="SignalRClientService"/> class.</summary>
    public SignalRClientService(
        IConfiguration configuration,
        IUiDispatcher dispatcher,
        TimeProvider timeProvider,
        Lazy<BackgroundScheduler> schedulerFactory)
        : this(configuration, dispatcher, timeProvider, schedulerFactory, new SignalRClientAdapter(), DefaultRetrySchedule)
    {
    }

    internal SignalRClientService(
        IConfiguration configuration,
        IUiDispatcher dispatcher,
        TimeProvider timeProvider,
        Lazy<BackgroundScheduler> schedulerFactory,
        ISignalRClientAdapter adapter,
        IReadOnlyList<TimeSpan> retrySchedule)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _schedulerFactory = schedulerFactory ?? throw new ArgumentNullException(nameof(schedulerFactory));
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _retrySchedule = retrySchedule ?? throw new ArgumentNullException(nameof(retrySchedule));
        if (_retrySchedule.Count == 0)
        {
            throw new ArgumentException("Retry schedule must contain at least one entry.", nameof(retrySchedule));
        }

        _hubUrl = ResolveHubUrl(configuration);

        _adapter.AuditReceived += OnAuditReceivedAsync;
    }

    /// <inheritdoc />
    public event EventHandler<AuditEventArgs>? AuditReceived;

    /// <inheritdoc />
    public event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <inheritdoc />
    public RealtimeConnectionState ConnectionState { get; private set; } = RealtimeConnectionState.Disconnected;

    /// <inheritdoc />
    public string? LastError { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? NextRetryUtc { get; private set; }

    /// <summary>Starts the connection loop and ensures the background scheduler is created.</summary>
    public void Start()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SignalRClientService));
            }

            if (_runTask != null)
            {
                return;
            }

            _ = _schedulerFactory.Value; // Ensure scheduler is instantiated.
            _runTask = Task.Run(() => RunAsync(_cts.Token));
        }
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var connected = await AttemptConnectAsync(token).ConfigureAwait(false);
            if (!connected)
            {
                continue;
            }

            await WaitForDisconnectAsync(token).ConfigureAwait(false);
        }
    }

    private async Task<bool> AttemptConnectAsync(CancellationToken token)
    {
        var delay = _retrySchedule[Math.Min(_attempt, _retrySchedule.Count - 1)];
        if (delay > TimeSpan.Zero)
        {
            var next = _timeProvider.GetUtcNow() + delay;
            await _dispatcher.InvokeAsync(() =>
            {
                NextRetryUtc = next;
                UpdateState(RealtimeConnectionState.Retrying, LastError);
            }).ConfigureAwait(false);

            try
            {
                await _timeProvider.Delay(delay, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }

        await _dispatcher.InvokeAsync(() =>
        {
            NextRetryUtc = null;
            UpdateState(RealtimeConnectionState.Connecting, null);
        }).ConfigureAwait(false);

        try
        {
            await _adapter.ConnectAsync(_hubUrl, token).ConfigureAwait(false);
            _attempt = 0;
            await _dispatcher.InvokeAsync(() =>
            {
                LastError = null;
                UpdateState(RealtimeConnectionState.Connected, null);
            }).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex) when (!token.IsCancellationRequested)
        {
            _attempt++;
            await _dispatcher.InvokeAsync(() =>
            {
                LastError = ex.Message;
                UpdateState(RealtimeConnectionState.Disconnected, LastError);
            }).ConfigureAwait(false);
            return false;
        }
    }

    private async Task WaitForDisconnectAsync(CancellationToken token)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task ClosedHandler(Exception? ex)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                LastError = ex?.Message;
                UpdateState(RealtimeConnectionState.Disconnected, LastError);
            }).ConfigureAwait(false);

            completion.TrySetResult();
        }

        async Task ReconnectingHandler(Exception? ex)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                LastError = ex?.Message;
                UpdateState(RealtimeConnectionState.Retrying, LastError);
            }).ConfigureAwait(false);
        }

        async Task ReconnectedHandler(string? id)
        {
            await _dispatcher.InvokeAsync(() =>
            {
                LastError = null;
                UpdateState(RealtimeConnectionState.Connected, null);
            }).ConfigureAwait(false);
        }

        _adapter.ConnectionClosed += ClosedHandler;
        _adapter.Reconnecting += ReconnectingHandler;
        _adapter.Reconnected += ReconnectedHandler;

        using var registration = token.Register(() => completion.TrySetCanceled(token));

        try
        {
            await completion.Task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested – exit gracefully.
        }
        finally
        {
            _adapter.ConnectionClosed -= ClosedHandler;
            _adapter.Reconnecting -= ReconnectingHandler;
            _adapter.Reconnected -= ReconnectedHandler;
            _attempt = 1; // Apply at least the first backoff on subsequent retries.
        }
    }

    private Task OnAuditReceivedAsync(AuditEntryDto audit)
    {
        if (audit is null)
        {
            return Task.CompletedTask;
        }

        return _dispatcher.InvokeAsync(() =>
        {
            AuditReceived?.Invoke(this, new AuditEventArgs(audit));
        });
    }

    private void UpdateState(RealtimeConnectionState state, string? message)
    {
        ConnectionState = state;
        ConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs(state, message, NextRetryUtc));
    }

    private static string ResolveHubUrl(IConfiguration configuration)
    {
        if (configuration is null)
        {
            return DefaultHubUrl;
        }

        return configuration["SignalR:AuditHubUrl"]
               ?? configuration["Realtime:AuditHubUrl"]
               ?? configuration["Realtime:HubUrl"]
               ?? DefaultHubUrl;
    }

    private static IReadOnlyList<TimeSpan> DefaultRetrySchedule => new[]
    {
        TimeSpan.Zero,
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(1)
    };

    private const string DefaultHubUrl = "https://localhost:5001/hubs/audit";

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cts.Cancel();

        _adapter.AuditReceived -= OnAuditReceivedAsync;

        try
        {
            _runTask?.Wait(TimeSpan.FromSeconds(1));
        }
        catch
        {
            // Ignore shutdown faults.
        }
        finally
        {
            _cts.Dispose();
        }
    }

    private sealed class SignalRClientAdapter : ISignalRClientAdapter
    {
        public event Func<AuditEntryDto, Task>? AuditReceived
        {
            add => SignalRService.OnAuditReceived += value;
            remove => SignalRService.OnAuditReceived -= value;
        }

        public event Func<Exception?, Task>? ConnectionClosed
        {
            add => SignalRService.OnConnectionClosed += value;
            remove => SignalRService.OnConnectionClosed -= value;
        }

        public event Func<Exception?, Task>? Reconnecting
        {
            add => SignalRService.OnReconnecting += value;
            remove => SignalRService.OnReconnecting -= value;
        }

        public event Func<string?, Task>? Reconnected
        {
            add => SignalRService.OnReconnected += value;
            remove => SignalRService.OnReconnected -= value;
        }

        public Task ConnectAsync(string hubUrl, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return SignalRService.InitializeAsync(hubUrl);
        }
    }
}

/// <summary>Represents the connection states observed by <see cref="SignalRClientService"/>.</summary>
public enum RealtimeConnectionState
{
    /// <summary>The connection has not been established.</summary>
    Disconnected,

    /// <summary>The client is actively connecting.</summary>
    Connecting,

    /// <summary>The connection is established.</summary>
    Connected,

    /// <summary>The client is waiting to retry or is reconnecting.</summary>
    Retrying
}

/// <summary>Event payload describing a received audit entry.</summary>
public sealed class AuditEventArgs : EventArgs
{
    public AuditEventArgs(AuditEntryDto audit) => Audit = audit ?? throw new ArgumentNullException(nameof(audit));

    /// <summary>Gets the audit entry delivered by SignalR.</summary>
    public AuditEntryDto Audit { get; }
}

/// <summary>Event payload describing connection state changes.</summary>
public sealed class ConnectionStateChangedEventArgs : EventArgs
{
    public ConnectionStateChangedEventArgs(RealtimeConnectionState state, string? message, DateTimeOffset? nextRetryUtc)
    {
        State = state;
        Message = message;
        NextRetryUtc = nextRetryUtc;
    }

    /// <summary>Gets the new connection state.</summary>
    public RealtimeConnectionState State { get; }

    /// <summary>Gets the last known error message, if any.</summary>
    public string? Message { get; }

    /// <summary>Gets the next scheduled retry timestamp (UTC).</summary>
    public DateTimeOffset? NextRetryUtc { get; }
}

/// <summary>Public abstraction consumed by view-models to observe live updates.</summary>
public interface ISignalRClientService
{
    /// <summary>Raised when a new audit entry is pushed from the server.</summary>
    event EventHandler<AuditEventArgs>? AuditReceived;

    /// <summary>Raised when the connection state changes.</summary>
    event EventHandler<ConnectionStateChangedEventArgs>? ConnectionStateChanged;

    /// <summary>Gets the current connection state.</summary>
    RealtimeConnectionState ConnectionState { get; }

    /// <summary>Gets the last observed error message.</summary>
    string? LastError { get; }

    /// <summary>Gets the next scheduled retry timestamp (UTC).</summary>
    DateTimeOffset? NextRetryUtc { get; }

    /// <summary>Starts the client and ensures background services are active.</summary>
    void Start();
}

internal interface ISignalRClientAdapter
{
    event Func<AuditEntryDto, Task>? AuditReceived;
    event Func<Exception?, Task>? ConnectionClosed;
    event Func<Exception?, Task>? Reconnecting;
    event Func<string?, Task>? Reconnected;

    Task ConnectAsync(string hubUrl, CancellationToken token);
}
