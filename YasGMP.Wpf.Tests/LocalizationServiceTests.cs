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
        Assert.Equal("Find", _service.GetString("Module.Toolbar.Toggle.Find.Content"));
    }

    [Fact]
    public void SetLanguage_ToCroatian_ProvidesExpectedRibbonAndToolbarStrings()
    {
        _service.SetLanguage("hr");

        Assert.Equal("Početna", _service.GetString("Ribbon.Tab.Home.Header"));
        Assert.Equal("Moduli", _service.GetString("Dock.Modules.Title"));
        Assert.Equal("Traži", _service.GetString("Module.Toolbar.Toggle.Find.Content"));
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
