using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.Core.WindowsAPI;
using FlaUI.Core.Input;
using Xunit;
using Xunit.Sdk;
using YasGMP.Wpf.Smoke.Helpers;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Smoke.Tests;

public class QualityModulesSmokeTests
{
    [Fact]
    public async Task IncidentsModule_WorkflowTransitionsAndInspectorTimelineAsync()
    {
        await RunQualityModuleScenarioAsync(
            "ModuleTree.Node.Quality.Incidents.AutomationId",
            "Module.Title.Incidents");
    }

    [Fact]
    public async Task CapaModule_WorkflowTransitionsAndInspectorTimelineAsync()
    {
        await RunQualityModuleScenarioAsync(
            "ModuleTree.Node.Quality.Capa.AutomationId",
            "Module.Title.Capa");
    }

    [Fact]
    public async Task ChangeControlModule_WorkflowTransitionsAndInspectorTimelineAsync()
    {
        await RunQualityModuleScenarioAsync(
            "ModuleTree.Node.Quality.ChangeControl.AutomationId",
            "Module.Title.ChangeControl");
    }

    private static async Task RunQualityModuleScenarioAsync(string moduleNodeKey, string moduleTitleKey)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new SkipException("FlaUI smoke automation requires Windows host.");
        }

        var localization = LocalizationTestContext.ResolveLocalizationService();

        await using var session = await WpfApplicationSession.LaunchAsync();
        var window = session.MainWindow;

        localization.SetLanguage("en");
        SmokeAutomationClient.SetLanguage(window, "en");
        session.WaitForIdle();

        OpenQualityModule(window, localization, session, moduleNodeKey);
        session.WaitForIdle();

        var statusAutomationId = localization.GetString("Shell.StatusBar.Status.Value.AutomationId");
        WaitForNonEmptyStatus(window, statusAutomationId);

        SelectFirstRecord(window);
        session.WaitForIdle();

        ValidateInspector(window, localization, moduleTitleKey);

        StepThroughFormModes(window, session, localization);
    }

    private static void OpenQualityModule(
        Window window,
        ILocalizationService localization,
        WpfApplicationSession session,
        string moduleNodeKey)
    {
        var modulesPaneId = localization.GetString("Dock.Modules.AutomationId");
        var modulesPane = WaitForElement(window, modulesPaneId);

        var qualityCategoryId = localization.GetString("ModuleTree.Category.Quality.AutomationId");
        var qualityNode = WaitForDescendant(modulesPane, qualityCategoryId, ControlType.TreeItem).AsTreeItem();
        if (qualityNode.Patterns.ExpandCollapse.IsSupported
            && qualityNode.Patterns.ExpandCollapse.Pattern.ExpandCollapseState != ExpandCollapseState.Expanded)
        {
            qualityNode.Patterns.ExpandCollapse.Pattern.Expand();
            session.WaitForIdle();
        }

        var moduleAutomationId = localization.GetString(moduleNodeKey);
        var moduleNode = WaitForDescendant(qualityNode, moduleAutomationId, ControlType.TreeItem);
        moduleNode.Focus();
        moduleNode.Patterns.SelectionItem?.Select();
        Keyboard.Type(VirtualKeyShort.RETURN);
        session.WaitForIdle();

        WaitForFirstDataGrid(window);
    }

    private static void SelectFirstRecord(Window window)
    {
        var grid = WaitForFirstDataGrid(window);
        Retry.WhileTrue(
            () =>
            {
                var rows = grid.FindAllDescendants(cf => cf.ByControlType(ControlType.DataItem));
                if (rows.Length == 0)
                {
                    return true;
                }

                rows[0].Patterns.SelectionItem?.Select();
                return false;
            },
            timeout: TimeSpan.FromSeconds(10),
            throwOnTimeout: true);
    }

    private static void ValidateInspector(Window window, ILocalizationService localization, string moduleTitleKey)
    {
        var inspectorId = localization.GetString("Dock.Inspector.AutomationId");
        var inspector = WaitForElement(window, inspectorId);

        var moduleTitle = localization.GetString(moduleTitleKey);
        var moduleHeader = WaitForHeader(inspector, "Dock.Inspector.ModuleHeader");
        Retry.WhileTrue(
            () => !string.Equals(ReadText(moduleHeader), moduleTitle, StringComparison.Ordinal),
            timeout: TimeSpan.FromSeconds(10),
            throwOnTimeout: true);

        var recordHeader = WaitForHeader(inspector, "Dock.Inspector.RecordHeader");
        var recordTitle = ReadText(recordHeader);
        Assert.False(string.IsNullOrWhiteSpace(recordTitle));

        var moduleToken = AutomationIdSanitizer.Normalize(moduleTitle, "module");
        var recordToken = AutomationIdSanitizer.Normalize(recordTitle, "record");
        var fieldPrefix = $"Dock.Inspector.{moduleToken}.{recordToken}.";

        Retry.WhileTrue(
            () =>
            {
                var fieldRows = inspector
                    .FindAllDescendants(cf => cf.ByControlType(ControlType.Pane))
                    .Where(e => e.AutomationId is { } id && id.StartsWith(fieldPrefix, StringComparison.Ordinal))
                    .ToArray();
                return fieldRows.Length == 0;
            },
            timeout: TimeSpan.FromSeconds(10),
            throwOnTimeout: true);
    }

    private static void StepThroughFormModes(Window window, WpfApplicationSession session, ILocalizationService localization)
    {
        ToggleToolbarButton(window, localization.GetString("Module.Toolbar.Toggle.Find.AutomationId"));
        session.WaitForIdle();

        ToggleToolbarButton(window, localization.GetString("Module.Toolbar.Toggle.Add.AutomationId"));
        session.WaitForIdle();

        InvokeToolbarButton(window, localization.GetString("Module.Toolbar.Command.Cancel.AutomationId"));
        session.WaitForIdle();

        ToggleToolbarButton(window, localization.GetString("Module.Toolbar.Toggle.Update.AutomationId"));
        session.WaitForIdle();

        InvokeToolbarButton(window, localization.GetString("Module.Toolbar.Command.Cancel.AutomationId"));
        session.WaitForIdle();

        ToggleToolbarButton(window, localization.GetString("Module.Toolbar.Toggle.View.AutomationId"));
        session.WaitForIdle();
    }

    private static void ToggleToolbarButton(Window window, string automationId)
    {
        var button = WaitForElement(window, automationId);
        if (button.Patterns.Toggle.IsSupported)
        {
            button.Patterns.Toggle.Pattern.Toggle();
        }
        else
        {
            button.AsButton().Invoke();
        }
    }

    private static void InvokeToolbarButton(Window window, string automationId)
    {
        var button = WaitForElement(window, automationId, ControlType.Button).AsButton();
        button.Invoke();
    }

    private static AutomationElement WaitForFirstDataGrid(Window window)
    {
        return Retry.WhileNull(
                () => window.FindFirstDescendant(cf => cf.ByControlType(ControlType.DataGrid)),
                timeout: TimeSpan.FromSeconds(30),
                throwOnTimeout: true)
            .Result;
    }

    private static AutomationElement WaitForHeader(AutomationElement parent, string automationId)
        => WaitForElement(parent, automationId, ControlType.Text);

    private static void WaitForNonEmptyStatus(Window window, string automationId)
    {
        Retry.WhileTrue(
            () =>
            {
                var element = WaitForElement(window, automationId, ControlType.Text);
                var text = ReadText(element);
                return string.IsNullOrWhiteSpace(text);
            },
            timeout: TimeSpan.FromSeconds(15),
            throwOnTimeout: true);
    }

    private static AutomationElement WaitForElement(Window window, string automationId, ControlType? controlType = null)
        => WaitForElement(window as AutomationElement, automationId, controlType);

    private static AutomationElement WaitForElement(AutomationElement parent, string automationId, ControlType? controlType = null)
    {
        var cf = parent.Automation.ConditionFactory;
        return Retry.WhileNull(
                () => parent.FindFirstDescendant(controlType is null
                    ? cf.ByAutomationId(automationId)
                    : cf.ByAutomationId(automationId).And(cf.ByControlType(controlType.Value))),
                timeout: TimeSpan.FromSeconds(30),
                throwOnTimeout: true)
            .Result;
    }

    private static AutomationElement WaitForDescendant(AutomationElement parent, string automationId, ControlType? controlType = null)
        => WaitForElement(parent, automationId, controlType);

    private static string ReadText(AutomationElement element)
    {
        if (element.Patterns.Text.IsSupported)
        {
            var text = element.Patterns.Text.Pattern.DocumentRange.GetText(int.MaxValue);
            return text.TrimEnd('\r', '\n');
        }

        if (element.Patterns.Value.IsSupported)
        {
            return element.Patterns.Value.Pattern.Value ?? string.Empty;
        }

        return element.Name ?? string.Empty;
    }
}
