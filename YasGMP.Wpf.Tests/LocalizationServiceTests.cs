using System;
using System.Linq;
using Xunit;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;

namespace YasGMP.Wpf.Tests;

public sealed class LocalizationServiceTests : IDisposable
{
    private readonly LocalizationService _service;
    private readonly string _originalLanguage;

    public LocalizationServiceTests()
    {
        _service = new LocalizationService();
        _originalLanguage = _service.CurrentLanguage;
    }

    [Fact]
    public void SetLanguage_ToEnglish_ProvidesExpectedRibbonAndToolbarStrings()
    {
        _service.SetLanguage("en");

        Assert.Equal("Home", _service.GetString("Ribbon.Tab.Home.Header"));
        Assert.Equal("Modules", _service.GetString("Dock.Modules.Title"));
        Assert.Equal("Review the active module context and selected record details.", _service.GetString("Dock.Inspector.ToolTip"));
        Assert.Equal("Find", _service.GetString("Module.Toolbar.Toggle.Find.Content"));
        Assert.Equal("Smoke test already running.", _service.GetString("Shell.Status.Smoke.AlreadyRunning"));
        Assert.Equal(
            "3/5 smoke checks succeeded. Log written to C\\logs\\output.log.",
            _service.GetString("Shell.Status.Smoke.Result.WithLog", 3, 5, "C\\logs\\output.log"));
        Assert.Equal(
            "3/5 smoke checks succeeded. Failed to persist log: disk full",
            _service.GetString("Shell.Status.Smoke.Result.LogFailure", 3, 5, "disk full"));
        Assert.Equal("Signature Metadata", _service.GetString("SignatureMetadata.Group.Header"));
        Assert.Equal("Production KPIs", _service.GetString("Cockpit.Section.Metrics.Header"));
        Assert.Equal("User", _service.GetString("Audit.Filter.User.Label"));
        Assert.Equal("Audit log is loading.", _service.GetString("Audit.Progress.Indicator.ToolTip"));
    }

    [Fact]
    public void SetLanguage_ToCroatian_ProvidesExpectedRibbonAndToolbarStrings()
    {
        _service.SetLanguage("hr");

        Assert.Equal("Početna", _service.GetString("Ribbon.Tab.Home.Header"));
        Assert.Equal("Moduli", _service.GetString("Dock.Modules.Title"));
        Assert.Equal("Pregledaj aktivni kontekst modula i detalje odabranog zapisa.", _service.GetString("Dock.Inspector.ToolTip"));
        Assert.Equal("Traži", _service.GetString("Module.Toolbar.Toggle.Find.Content"));
        Assert.Equal("Smoke test već je u tijeku.", _service.GetString("Shell.Status.Smoke.AlreadyRunning"));
        Assert.Equal(
            "3/5 provjera dima je uspješno završilo. Zapis je spremljen u C\\logs\\output.log.",
            _service.GetString("Shell.Status.Smoke.Result.WithLog", 3, 5, "C\\logs\\output.log"));
        Assert.Equal(
            "3/5 provjera dima je uspješno završilo. Spremanje zapisa nije uspjelo: disk full",
            _service.GetString("Shell.Status.Smoke.Result.LogFailure", 3, 5, "disk full"));
        Assert.Equal("Metapodaci potpisa", _service.GetString("SignatureMetadata.Group.Header"));
        Assert.Equal("Proizvodni KPI pokazatelji", _service.GetString("Cockpit.Section.Metrics.Header"));
        Assert.Equal("Korisnik", _service.GetString("Audit.Filter.User.Label"));
        Assert.Equal("Revizijski dnevnik se učitava.", _service.GetString("Audit.Progress.Indicator.ToolTip"));
    }

    [Fact]
    public void ModuleTreeViewModel_UpdatesLocalizedTextAfterLanguageSwitch()
    {
        _service.SetLanguage("en");
        var viewModel = new ModuleTreeViewModel(_service);

        Assert.Equal("Modules", viewModel.Title);
        Assert.Equal("Module tree navigation", viewModel.AutomationName);
        Assert.Equal("Browse modules by category.", viewModel.ToolTip);
        Assert.True(viewModel.Modules.Any());
        Assert.Equal("Quality", viewModel.Modules.First().Title);

        _service.SetLanguage("hr");

        Assert.Equal("Moduli", viewModel.Title);
        Assert.Equal("Navigacija stablom modula", viewModel.AutomationName);
        Assert.Equal("Pregledavaj module po kategoriji.", viewModel.ToolTip);
        Assert.True(viewModel.Modules.Any());
        Assert.Equal("Kvaliteta", viewModel.Modules.First().Title);
    }

    public void Dispose() => _service.SetLanguage(_originalLanguage);
}
