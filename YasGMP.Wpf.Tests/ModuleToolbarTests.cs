using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using CommunityToolkit.Mvvm.Input;
using Xunit;
using YasGMP.Wpf.Tests.TestDoubles;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public sealed class ModuleToolbarTests
{
    [Fact]
    public void ToggleTemplate_RefreshesToolTipAndAutomationMetadata_AfterLanguageChange()
    {
        RunOnStaThread(() =>
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

            var resources = new ResourceDictionary
            {
                Source = new Uri(
                    "pack://application:,,,/YasGMP.Wpf;component/Resources/ModuleToolbarResources.xaml",
                    UriKind.Absolute)
            };

            var template = Assert.IsType<DataTemplate>(resources["ModuleToolbarToggleButtonTemplate"]);
            var toggle = Assert.IsType<ToggleButton>(template.LoadContent());
            toggle.DataContext = command;
            toggle.ApplyTemplate();

            Assert.Equal("Find", toggle.Content);
            Assert.True(ToolTipService.GetShowOnDisabled(toggle));
            Assert.Equal("Locate an existing record.", ToolTipService.GetToolTip(toggle));
            Assert.Equal("Find command", AutomationProperties.GetName(toggle));
            Assert.Equal("Toolbar_Find", AutomationProperties.GetAutomationId(toggle));

            localization.SetLanguage("hr");
            toggle.Dispatcher.Invoke(() => { });

            Assert.Equal("Traži", toggle.Content);
            Assert.Equal("Pronađi postojeći zapis.", ToolTipService.GetToolTip(toggle));
            Assert.Equal("Naredba traži", AutomationProperties.GetName(toggle));
            Assert.Equal("AlatnaTraka_Trazi", AutomationProperties.GetAutomationId(toggle));
        });
    }

    private static void RunOnStaThread(Action action)
    {
        if (action is null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        })
        {
            IsBackground = true,
            Name = "ModuleToolbar STA test"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            ExceptionDispatchInfo.Capture(captured).Throw();
        }
    }
}
