using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using Xunit;
using YasGMP.Wpf.ViewModels.Modules;
using YasGMP.Wpf.Tests.TestDoubles;

namespace YasGMP.Wpf.Tests;

public sealed class ModuleToolbarCommandTests
{
    [Fact]
    public void LanguageChange_RehydratesLocalizedProperties()
    {
        var localization = new FakeLocalizationService(
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Module.Toolbar.Toggle.Find.Content"] = "Find",
                    ["Module.Toolbar.Toggle.Find.ToolTip"] = "Locate an existing record.",
                    ["Module.Toolbar.Toggle.Find.AutomationName"] = "Find command",
                    ["Module.Toolbar.Toggle.Find.AutomationId"] = "Toolbar_Find"
                },
                ["hr"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Module.Toolbar.Toggle.Find.Content"] = "Traži",
                    ["Module.Toolbar.Toggle.Find.ToolTip"] = "Pronađi postojeći zapis.",
                    ["Module.Toolbar.Toggle.Find.AutomationName"] = "Naredba traži",
                    ["Module.Toolbar.Toggle.Find.AutomationId"] = "AlatnaTraka_Trazi"
                }
            },
            initialLanguage: "en");

        var command = new ModuleToolbarCommand(
            captionKey: "Module.Toolbar.Toggle.Find.Content",
            command: new RelayCommand(() => { }),
            localization: localization,
            toolTipKey: "Module.Toolbar.Toggle.Find.ToolTip",
            automationNameKey: "Module.Toolbar.Toggle.Find.AutomationName",
            automationIdKey: "Module.Toolbar.Toggle.Find.AutomationId");

        Assert.Equal("Find", command.Caption);
        Assert.Equal("Locate an existing record.", command.ToolTip);
        Assert.Equal("Find command", command.AutomationName);
        Assert.Equal("Toolbar_Find", command.AutomationId);

        localization.SetLanguage("hr");

        Assert.Equal("Traži", command.Caption);
        Assert.Equal("Pronađi postojeći zapis.", command.ToolTip);
        Assert.Equal("Naredba traži", command.AutomationName);
        Assert.Equal("AlatnaTraka_Trazi", command.AutomationId);
    }

    [Fact]
    public void LanguageChange_NotifiesCaptionTooltipAndAutomationProperties()
    {
        var localization = new FakeLocalizationService(
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Module.Toolbar.Toggle.Add.Content"] = "Add",
                    ["Module.Toolbar.Toggle.Add.ToolTip"] = "Create a new record.",
                    ["Module.Toolbar.Toggle.Add.AutomationName"] = "Add command",
                    ["Module.Toolbar.Toggle.Add.AutomationId"] = "Toolbar_Add"
                },
                ["hr"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    ["Module.Toolbar.Toggle.Add.Content"] = "Dodaj",
                    ["Module.Toolbar.Toggle.Add.ToolTip"] = "Kreiraj novi zapis.",
                    ["Module.Toolbar.Toggle.Add.AutomationName"] = "Naredba dodaj",
                    ["Module.Toolbar.Toggle.Add.AutomationId"] = "AlatnaTraka_Dodaj"
                }
            },
            initialLanguage: "en");

        var command = new ModuleToolbarCommand(
            captionKey: "Module.Toolbar.Toggle.Add.Content",
            command: new RelayCommand(() => { }),
            localization: localization,
            toolTipKey: "Module.Toolbar.Toggle.Add.ToolTip",
            automationNameKey: "Module.Toolbar.Toggle.Add.AutomationName",
            automationIdKey: "Module.Toolbar.Toggle.Add.AutomationId");

        var observed = new List<string>();
        command.PropertyChanged += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.PropertyName))
            {
                observed.Add(args.PropertyName);
            }
        };

        localization.SetLanguage("hr");

        Assert.Contains(nameof(ModuleToolbarCommand.Caption), observed);
        Assert.Contains(nameof(ModuleToolbarCommand.ToolTip), observed);
        Assert.Contains(nameof(ModuleToolbarCommand.AutomationName), observed);
        Assert.Contains(nameof(ModuleToolbarCommand.AutomationId), observed);
    }

    
}
