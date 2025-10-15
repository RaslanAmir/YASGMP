using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Tests;

public sealed class SignalRClientServiceTests
{
    [Fact]
    public async Task Start_RetriesUntilConnected()
    {
        var adapter = new FakeSignalRClientAdapter();
        adapter.EnqueueResult(new InvalidOperationException("fail-1"));
        adapter.EnqueueResult(new InvalidOperationException("fail-2"));
        adapter.EnqueueResult(null);

        var time = new RecordingTimeProvider();
        using var service = CreateService(adapter, time);

        service.Start();

        await WaitForAsync(() => adapter.Attempts >= 3, TimeSpan.FromSeconds(1));

        Assert.Equal(RealtimeConnectionState.Connected, service.ConnectionState);
        Assert.Equal(new[] { TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15) }, time.Delays);
    }

    [Fact]
    public async Task AuditEvent_RaisedThroughDispatcher()
    {
        var adapter = new FakeSignalRClientAdapter();
        adapter.EnqueueResult(null);
        var time = new RecordingTimeProvider();
        using var service = CreateService(adapter, time);

        var raised = false;
        service.AuditReceived += (_, args) =>
        {
            Assert.NotNull(args.Audit);
            raised = true;
        };

        service.Start();
        await WaitForAsync(() => adapter.Attempts >= 1, TimeSpan.FromSeconds(1));
        await adapter.RaiseAuditAsync(new AuditEntryDto());

        Assert.True(raised);
    }

    [Fact]
    public async Task ClosedEvent_TriggersReconnectWithBackoff()
    {
        var adapter = new FakeSignalRClientAdapter();
        adapter.EnqueueResult(null); // initial connection
        adapter.EnqueueResult(new InvalidOperationException("network"));
        adapter.EnqueueResult(null); // reconnect success

        var time = new RecordingTimeProvider();
        using var service = CreateService(adapter, time);

        service.Start();
        await WaitForAsync(() => adapter.Attempts >= 1, TimeSpan.FromSeconds(1));

        await adapter.RaiseClosedAsync(new Exception("lost"));
        await WaitForAsync(() => adapter.Attempts >= 3, TimeSpan.FromSeconds(1));

        Assert.Equal(RealtimeConnectionState.Connected, service.ConnectionState);
        Assert.Contains(TimeSpan.FromSeconds(5), time.Delays);
    }

    private static SignalRClientService CreateService(FakeSignalRClientAdapter adapter, RecordingTimeProvider time)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SignalR:AuditHubUrl"] = "https://localhost/hub"
            })
            .Build();

        var schedulerFactory = new Lazy<BackgroundScheduler>(() =>
            (BackgroundScheduler)FormatterServices.GetUninitializedObject(typeof(BackgroundScheduler)));

        return new SignalRClientService(
            configuration,
            new ImmediateUiDispatcher(),
            time,
            schedulerFactory,
            adapter,
            new[]
            {
                TimeSpan.Zero,
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(15)
            });
    }

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - start > timeout)
            {
                throw new TimeoutException("Condition was not satisfied within the allotted time.");
            }

            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    private sealed class FakeSignalRClientAdapter : ISignalRClientAdapter
    {
        private readonly Queue<Exception?> _results = new();

        public int Attempts => _attempts;

        private int _attempts;

        public event Func<AuditEntryDto, Task>? AuditReceived;
        public event Func<Exception?, Task>? ConnectionClosed;
        public event Func<Exception?, Task>? Reconnecting;
        public event Func<string?, Task>? Reconnected;

        public void EnqueueResult(Exception? result)
            => _results.Enqueue(result);

        public Task ConnectAsync(string hubUrl, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _attempts++;

            if (_results.Count > 0)
            {
                var outcome = _results.Dequeue();
                if (outcome != null)
                {
                    throw outcome;
                }
            }

            return Task.CompletedTask;
        }

        public Task RaiseAuditAsync(AuditEntryDto audit)
            => AuditReceived?.Invoke(audit) ?? Task.CompletedTask;

        public Task RaiseClosedAsync(Exception? ex)
            => ConnectionClosed?.Invoke(ex) ?? Task.CompletedTask;

        public Task RaiseReconnectingAsync(Exception? ex)
            => Reconnecting?.Invoke(ex) ?? Task.CompletedTask;

        public Task RaiseReconnectedAsync(string? id)
            => Reconnected?.Invoke(id) ?? Task.CompletedTask;
    }

    private sealed class RecordingTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.UtcNow;

        public List<TimeSpan> Delays { get; } = new();

        public override DateTimeOffset GetUtcNow() => _now;

        public override ValueTask Delay(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            Delays.Add(delay);
            _now += delay;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ImmediateUiDispatcher : IUiDispatcher
    {
        public bool IsDispatchRequired => false;

        public void BeginInvoke(Action action) => action();

        public Task InvokeAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }

        public Task InvokeAsync(Func<Task> asyncAction) => asyncAction();

        public Task<T> InvokeAsync<T>(Func<T> func) => Task.FromResult(func());
    }
}
