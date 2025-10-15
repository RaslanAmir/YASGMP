using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Ui;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;
using YasGMP.Wpf.Tests.TestDoubles;

namespace YasGMP.Wpf.Tests;

public class AlertServiceTests
{
    [Fact]
    public async Task AlertAsync_PublishesToastAndStatus()
    {
        var shell = new StubShellInteractionService();
        var preferences = new TestNotificationPreferenceService();
        var dispatcher = new ImmediateDispatcher();
        var service = new AlertService(shell, preferences, dispatcher);

        await service.AlertAsync("Info", "Saved", "OK");

        Assert.Equal("Info: Saved", shell.LastStatus);
        Assert.Single(service.Toasts);
        Assert.Equal("Info: Saved", service.Toasts[0].Message);
    }

    [Fact]
    public void PublishStatus_DisabledPreferences_SuppressesToastAndStatus()
    {
        var shell = new StubShellInteractionService();
        var preferences = new TestNotificationPreferenceService(new NotificationPreferences
        {
            ShowStatusBarAlerts = false,
            ShowToastAlerts = false,
        });
        var dispatcher = new ImmediateDispatcher();
        var service = new AlertService(shell, preferences, dispatcher);

        service.PublishStatus("Saved", AlertSeverity.Success);

        Assert.Null(shell.LastStatus);
        Assert.Empty(service.Toasts);
    }

    [Fact]
    public void PublishStatus_AddsToast_WhenEnabled()
    {
        var shell = new StubShellInteractionService();
        var preferences = new TestNotificationPreferenceService();
        var dispatcher = new ImmediateDispatcher();
        var service = new AlertService(shell, preferences, dispatcher);

        service.PublishStatus("Saved", AlertSeverity.Success, propagateToStatusBar: false);

        Assert.Equal("Saved", shell.LastStatus);
        Assert.Single(service.Toasts);
        Assert.Equal(AlertSeverity.Success, service.Toasts[0].Severity);
    }

    [Fact]
    public void PreferencesChanged_DisablesExistingToasts()
    {
        var shell = new StubShellInteractionService();
        var preferences = new TestNotificationPreferenceService();
        var dispatcher = new ImmediateDispatcher();
        var service = new AlertService(shell, preferences, dispatcher);

        service.PublishStatus("Saved", AlertSeverity.Success);
        Assert.Single(service.Toasts);

        preferences.TriggerChange(new NotificationPreferences
        {
            ShowStatusBarAlerts = true,
            ShowToastAlerts = false,
        });

        Assert.Empty(service.Toasts);
    }

    private sealed class TestNotificationPreferenceService : INotificationPreferenceService
    {
        public TestNotificationPreferenceService()
            : this(NotificationPreferences.CreateDefault())
        {
        }

        public TestNotificationPreferenceService(NotificationPreferences initial)
        {
            Current = initial.Clone();
        }

        public event EventHandler<NotificationPreferences>? PreferencesChanged;

        public NotificationPreferences Current { get; private set; }

        public Task<NotificationPreferences> ReloadAsync(CancellationToken token = default)
            => Task.FromResult(Current.Clone());

        public Task SaveAsync(NotificationPreferences preferences, CancellationToken token = default)
        {
            TriggerChange(preferences);
            return Task.CompletedTask;
        }

        public void TriggerChange(NotificationPreferences preferences)
        {
            Current = preferences.Clone();
            PreferencesChanged?.Invoke(this, Current.Clone());
        }
    }

    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        public bool IsDispatchRequired => false;

        public void BeginInvoke(Action action) => action();

        public Task InvokeAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }

        public Task<T> InvokeAsync<T>(Func<T> func) => Task.FromResult(func());

        public Task InvokeAsync(Func<Task> asyncAction) => asyncAction();
    }
}
