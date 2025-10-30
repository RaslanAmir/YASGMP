using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;

namespace YasGMP.Wpf.Tests;

public sealed class CockpitViewModelTests : IDisposable
{
    private readonly LocalizationService _localization = new();
    private readonly string _originalLanguage;

    public CockpitViewModelTests()
    {
        _originalLanguage = _localization.CurrentLanguage;
    }

    [Fact]
    public async Task RefreshAsync_PopulatesMetricsAndNoticesFromDatabase()
    {
        var database = new DatabaseService();
        database.KpiWidgets.Add(new KpiWidget
        {
            Title = "Open CAPAs",
            Value = 7,
            ValueText = "7",
            Color = "#c7522a",
            Trend = "up",
            IsAlert = true,
            LastUpdated = DateTime.UtcNow
        });
        database.KpiWidgets.Add(new KpiWidget
        {
            Title = "On-Time Deliveries",
            Value = 96,
            Unit = "%",
            ValueText = "96%",
            Trend = "down",
            Color = "#2e7d32",
            IsAlert = false,
            LastUpdated = DateTime.UtcNow
        });

        database.DashboardEvents.Add(new DashboardEvent
        {
            Id = 1,
            Description = "Sterilizer cycle expires in 2 days",
            Severity = "warning",
            Timestamp = DateTime.UtcNow.AddHours(-2)
        });
        database.DashboardEvents.Add(new DashboardEvent
        {
            Id = 2,
            EventType = "capa_pending",
            Description = "Two deviations awaiting approval",
            Severity = "info",
            Timestamp = DateTime.UtcNow.AddHours(-6)
        });

        var session = new StubUserSession(new User { Id = 42, Username = "qa" });
        var viewModel = new CockpitViewModel(database, _localization, session);

        await WaitForAsync(() => viewModel.HasMetrics && viewModel.HasNotices && !viewModel.IsLoading);

        Assert.Collection(
            viewModel.Metrics,
            metric =>
            {
                Assert.Equal("Open CAPAs", metric.Title);
                Assert.Equal("7", metric.Value);
                Assert.True(metric.IsAlert);
            },
            metric =>
            {
                Assert.Equal("On-Time Deliveries", metric.Title);
                Assert.Equal("96%", metric.Value);
                Assert.False(metric.IsAlert);
                Assert.True(metric.HasUnit);
            });

        Assert.Collection(
            viewModel.Notices,
            notice =>
            {
                Assert.Equal("Sterilizer cycle expires in 2 days", notice.Summary);
                Assert.Equal("warning", notice.Severity);
                Assert.True(notice.HasSeverity);
            },
            notice =>
            {
                Assert.Equal("Two deviations awaiting approval", notice.Summary);
                Assert.Equal("info", notice.Severity);
            });

        Assert.Equal(_localization.GetString("Cockpit.Status.Ready"), viewModel.StatusMessage);
    }

    [Fact]
    public async Task LanguageChange_RefreshesLocalizedContent_ForCroatian()
    {
        var database = new DatabaseService();
        database.KpiWidgets.Add(new KpiWidget
        {
            Title = "Preventive Jobs Due",
            Value = 12,
            ValueText = "12",
            Trend = "up",
            Color = "#d9842b",
            LastUpdated = DateTime.UtcNow
        });
        database.DashboardEvents.Add(new DashboardEvent
        {
            Id = 3,
            Description = "Calibration overdue for pump 12",
            Severity = "critical",
            Timestamp = DateTime.UtcNow
        });

        _localization.SetLanguage("en");
        var session = new StubUserSession(new User { Id = 7, Username = "operator" });
        var viewModel = new CockpitViewModel(database, _localization, session);
        await WaitForAsync(() => viewModel.HasMetrics && viewModel.HasNotices);
        var englishStatus = viewModel.StatusMessage;

        _localization.SetLanguage("hr");
        await WaitForAsync(() => string.Equals(viewModel.Title, _localization.GetString("Cockpit.Anchor.Title"), StringComparison.Ordinal));

        Assert.Equal(_localization.GetString("Cockpit.Anchor.AutomationId"), viewModel.AutomationId);
        Assert.Equal(_localization.GetString("Cockpit.Status.Ready"), viewModel.StatusMessage);
        Assert.NotEqual(englishStatus, viewModel.StatusMessage);
        Assert.Equal(1, viewModel.Metrics.Count);
        Assert.Equal(1, viewModel.Notices.Count);
    }

    public void Dispose() => _localization.SetLanguage(_originalLanguage);

    private static async Task WaitForAsync(Func<bool> condition, TimeSpan? timeout = null, int pollMilliseconds = 10)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(1));
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(pollMilliseconds).ConfigureAwait(false);
        }

        throw new TimeoutException("Condition was not satisfied before the timeout elapsed.");
    }

    private sealed class StubUserSession : IUserSession
    {
        private readonly User _user;

        public StubUserSession(User user)
        {
            _user = user ?? throw new ArgumentNullException(nameof(user));
            SessionId = Guid.NewGuid().ToString("N");
        }

        public User? CurrentUser => _user;

        public int? UserId => _user.Id;

        public string? Username => _user.Username;

        public string? FullName => _user.FullName ?? _user.Username;

        public string SessionId { get; }
    }
}
