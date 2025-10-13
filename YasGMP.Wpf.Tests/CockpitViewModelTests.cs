using System;
using System.Linq;
using Xunit;
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
    public void Constructor_SeedsLocalizedMetrics_InEnglish()
    {
        _localization.SetLanguage("en");

        var viewModel = new CockpitViewModel(_localization);

        Assert.Equal(_localization.GetString("Cockpit.Anchor.Title"), viewModel.Title);
        Assert.Equal(_localization.GetString("Cockpit.Anchor.AutomationId"), viewModel.AutomationId);
        Assert.Equal(
            new[]
            {
                _localization.GetString("Cockpit.Metric.OpenCapas.Label"),
                _localization.GetString("Cockpit.Metric.PreventiveJobsDue.Label"),
                _localization.GetString("Cockpit.Metric.MachinesOffline.Label"),
                _localization.GetString("Cockpit.Metric.OnTimeDeliveries.Label")
            },
            viewModel.Metrics.Select(m => m.Label).ToArray());
        Assert.Equal(
            new[]
            {
                _localization.GetString("Cockpit.Notice.SterilizerExpiring"),
                _localization.GetString("Cockpit.Notice.CalibrationOverdue"),
                _localization.GetString("Cockpit.Notice.DeviationsAwaitingApproval")
            },
            viewModel.Notices.ToArray());
    }

    [Fact]
    public void LanguageChange_RefreshesLocalizedContent_ForCroatian()
    {
        _localization.SetLanguage("en");
        var viewModel = new CockpitViewModel(_localization);
        var englishNotices = viewModel.Notices.ToArray();

        _localization.SetLanguage("hr");

        Assert.Equal(_localization.GetString("Cockpit.Anchor.Title"), viewModel.Title);
        Assert.Equal(_localization.GetString("Cockpit.Anchor.AutomationId"), viewModel.AutomationId);
        Assert.Equal(
            _localization.GetString("Cockpit.Metric.OpenCapas.Label"),
            viewModel.Metrics[0].Label);
        Assert.Equal(3, viewModel.Notices.Count);
        Assert.NotEqual(englishNotices[0], viewModel.Notices[0]);
        Assert.Equal(
            _localization.GetString("Cockpit.Notice.DeviationsAwaitingApproval"),
            viewModel.Notices[^1]);
    }

    public void Dispose() => _localization.SetLanguage(_originalLanguage);
}
