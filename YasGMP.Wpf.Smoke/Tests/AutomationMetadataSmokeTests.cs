using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using Xunit;
using Xunit.Sdk;
using YasGMP.Wpf.Smoke.Helpers;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;

namespace YasGMP.Wpf.Smoke.Tests;

public class AutomationMetadataSmokeTests
{
    [Fact]
    public async Task ModulesPaneAndInspectorAutomationMetadataFollowLocalizationChanges()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new SkipException("FlaUI smoke automation requires Windows host.");
        }

        var localization = LocalizationTestContext.ResolveLocalizationService();

        await using var session = await WpfApplicationSession.LaunchAsync();
        var window = session.MainWindow;

        // English baseline
        localization.SetLanguage("en");
        SmokeAutomationClient.SetLanguage(window, "en");
        session.WaitForIdle();

        ValidateModulesPane(window, localization);
        OpenDashboard(window, localization, session);

        var inspector = WaitForInspector(window, localization);
        var moduleTitle = localization.GetString("Module.Title.Dashboard");
        var activeState = ValidateInspectorActive(inspector, localization, moduleTitle);
        ValidateInspectorField(inspector, localization, moduleTitle, activeState.RecordTitle);

        SmokeAutomationClient.ResetInspector(window);
        session.WaitForIdle();
        ValidateInspectorPlaceholders(inspector, localization);

        // Restore active state for subsequent language checks.
        OpenDashboard(window, localization, session);
        inspector = WaitForInspector(window, localization);
        activeState = ValidateInspectorActive(inspector, localization, moduleTitle);
        ValidateInspectorField(inspector, localization, moduleTitle, activeState.RecordTitle);

        // Croatian verification without restarting the app.
        localization.SetLanguage("hr");
        SmokeAutomationClient.SetLanguage(window, "hr");
        session.WaitForIdle();

        ValidateModulesPane(window, localization);
        OpenDashboard(window, localization, session);

        inspector = WaitForInspector(window, localization);
        moduleTitle = localization.GetString("Module.Title.Dashboard");
        activeState = ValidateInspectorActive(inspector, localization, moduleTitle);
        ValidateInspectorField(inspector, localization, moduleTitle, activeState.RecordTitle);

        SmokeAutomationClient.ResetInspector(window);
        session.WaitForIdle();
        ValidateInspectorPlaceholders(inspector, localization);
    }

    private static void ValidateModulesPane(Window window, ILocalizationService localization)
    {
        var modulesAutomationId = localization.GetString("Dock.Modules.AutomationId");
        var modulesPane = WaitForElement(window, modulesAutomationId);
        Assert.Equal(modulesAutomationId, modulesPane.AutomationId);
        Assert.Equal(localization.GetString("Dock.Modules.AutomationName"), modulesPane.Name);
        Assert.Equal(localization.GetString("Dock.Modules.ToolTip"), GetHelpText(modulesPane));

        var cockpitAutomationId = localization.GetString("ModuleTree.Category.Cockpit.AutomationId");
        var cockpitGroup = WaitForDescendant(modulesPane, cockpitAutomationId);
        Assert.Equal(cockpitAutomationId, cockpitGroup.AutomationId);
        Assert.Equal(localization.GetString("ModuleTree.Category.Cockpit.AutomationName"), cockpitGroup.Name);
        Assert.Equal(localization.GetString("ModuleTree.Category.Cockpit.ToolTip"), GetHelpText(cockpitGroup));

        var dashboardAutomationId = localization.GetString("ModuleTree.Node.Cockpit.Dashboard.AutomationId");
        var dashboardButton = WaitForDescendant(cockpitGroup, dashboardAutomationId, ControlType.Button).AsButton();
        Assert.Equal(dashboardAutomationId, dashboardButton.AutomationId);
        Assert.Equal(localization.GetString("ModuleTree.Node.Cockpit.Dashboard.AutomationName"), dashboardButton.Name);
        Assert.Equal(localization.GetString("ModuleTree.Node.Cockpit.Dashboard.ToolTip"), GetHelpText(dashboardButton));
    }

    private static void OpenDashboard(Window window, ILocalizationService localization, WpfApplicationSession session)
    {
        var modulesAutomationId = localization.GetString("Dock.Modules.AutomationId");
        var modulesPane = WaitForElement(window, modulesAutomationId);
        var cockpitAutomationId = localization.GetString("ModuleTree.Category.Cockpit.AutomationId");
        var cockpitGroup = WaitForDescendant(modulesPane, cockpitAutomationId);
        var dashboardAutomationId = localization.GetString("ModuleTree.Node.Cockpit.Dashboard.AutomationId");
        var dashboardButton = WaitForDescendant(cockpitGroup, dashboardAutomationId, ControlType.Button).AsButton();
        dashboardButton.Invoke();
        session.WaitForIdle();
    }

    private static AutomationElement WaitForInspector(Window window, ILocalizationService localization)
    {
        var inspectorAutomationId = localization.GetString("Dock.Inspector.AutomationId");
        return WaitForElement(window, inspectorAutomationId);
    }

    private static (string ModuleTitle, string RecordTitle) ValidateInspectorActive(
        AutomationElement inspector,
        ILocalizationService localization,
        string moduleTitle)
    {
        var moduleHeader = WaitForHeader(inspector, "Dock.Inspector.ModuleHeader");
        var recordHeader = WaitForHeader(inspector, "Dock.Inspector.RecordHeader");

        var moduleAutomationIdTemplate = localization.GetString("Dock.Inspector.Module.AutomationId.Template");
        var moduleAutomationNameTemplate = localization.GetString("Dock.Inspector.Module.AutomationName.Template");
        var moduleAutomationTooltipTemplate = localization.GetString("Dock.Inspector.Module.ToolTip.Template");
        var moduleToken = AutomationIdSanitizer.Normalize(moduleTitle, "module");

        var expectedModuleAutomationId = string.Format(CultureInfo.InvariantCulture, moduleAutomationIdTemplate, moduleToken);
        Assert.Equal(expectedModuleAutomationId, moduleHeader.AutomationId);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, moduleAutomationNameTemplate, moduleTitle), moduleHeader.Name);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, moduleAutomationTooltipTemplate, moduleTitle), GetHelpText(moduleHeader));
        Assert.Equal(moduleTitle, ReadText(moduleHeader));

        var recordTitle = ReadText(recordHeader);
        var recordAutomationIdTemplate = localization.GetString("Dock.Inspector.Record.AutomationId.Template");
        var recordAutomationNameTemplate = localization.GetString("Dock.Inspector.Record.AutomationName.Template");
        var recordAutomationTooltipTemplate = localization.GetString("Dock.Inspector.Record.ToolTip.Template");
        var recordToken = AutomationIdSanitizer.Normalize(recordTitle, "record");

        var expectedRecordAutomationId = string.Format(CultureInfo.InvariantCulture, recordAutomationIdTemplate, recordToken);
        Assert.Equal(expectedRecordAutomationId, recordHeader.AutomationId);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, recordAutomationNameTemplate, recordTitle), recordHeader.Name);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, recordAutomationTooltipTemplate, recordTitle), GetHelpText(recordHeader));

        return (moduleTitle, recordTitle);
    }

    private static void ValidateInspectorField(
        AutomationElement inspector,
        ILocalizationService localization,
        string moduleTitle,
        string recordTitle)
    {
        var moduleToken = AutomationIdSanitizer.Normalize(moduleTitle, "module");
        var recordToken = AutomationIdSanitizer.Normalize(recordTitle, "record");
        var fieldPrefix = $"Dock.Inspector.{moduleToken}.{recordToken}.";

        AutomationElement? fieldRow = null;
        Retry.WhileNull(
            () =>
            {
                fieldRow = inspector
                    .FindAllDescendants(cf => cf.ByControlType(ControlType.Pane))
                    .FirstOrDefault(e => e.AutomationId is { } id && id.StartsWith(fieldPrefix, StringComparison.Ordinal));
                return fieldRow;
            },
            timeout: TimeSpan.FromSeconds(10),
            throwOnTimeout: true);

        if (fieldRow is null)
        {
            throw new InvalidOperationException("Inspector field row not found.");
        }

        var labelElement = fieldRow.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text));
        var labelText = ReadText(labelElement);
        var labelToken = AutomationIdSanitizer.Normalize(labelText, "field");

        var expectedAutomationId = $"{fieldPrefix}{labelToken}";
        Assert.Equal(expectedAutomationId, fieldRow.AutomationId);

        var automationNameTemplate = localization.GetString("Dock.Inspector.Field.AutomationName.Template");
        var automationTooltipTemplate = localization.GetString("Dock.Inspector.Field.AutomationTooltip.Template");
        Assert.Equal(
            string.Format(CultureInfo.CurrentCulture, automationNameTemplate, moduleTitle, labelText, recordTitle),
            fieldRow.Name);
        Assert.Equal(
            string.Format(CultureInfo.CurrentCulture, automationTooltipTemplate, labelText, recordTitle, moduleTitle),
            GetHelpText(fieldRow));
    }

    private static void ValidateInspectorPlaceholders(AutomationElement inspector, ILocalizationService localization)
    {
        var moduleHeader = WaitForHeader(inspector, "Dock.Inspector.ModuleHeader");
        var recordHeader = WaitForHeader(inspector, "Dock.Inspector.RecordHeader");

        Assert.Equal(localization.GetString("Dock.Inspector.Module.AutomationId"), moduleHeader.AutomationId);
        Assert.Equal(localization.GetString("Dock.Inspector.Module.AutomationName"), moduleHeader.Name);
        Assert.Equal(localization.GetString("Dock.Inspector.Module.ToolTip"), GetHelpText(moduleHeader));
        Assert.Equal(localization.GetString("Dock.Inspector.ModuleTitle"), ReadText(moduleHeader));

        var noRecord = localization.GetString("Dock.Inspector.NoRecord");
        var recordToken = AutomationIdSanitizer.Normalize(noRecord, "record");
        var recordAutomationIdTemplate = localization.GetString("Dock.Inspector.Record.AutomationId.Template");
        var recordAutomationNameTemplate = localization.GetString("Dock.Inspector.Record.AutomationName.Template");
        var recordAutomationTooltipTemplate = localization.GetString("Dock.Inspector.Record.ToolTip.Template");

        Assert.Equal(string.Format(CultureInfo.InvariantCulture, recordAutomationIdTemplate, recordToken), recordHeader.AutomationId);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, recordAutomationNameTemplate, noRecord), recordHeader.Name);
        Assert.Equal(string.Format(CultureInfo.CurrentCulture, recordAutomationTooltipTemplate, noRecord), GetHelpText(recordHeader));
        Assert.Equal(noRecord, ReadText(recordHeader));

        var fieldRows = inspector
            .FindAllDescendants(cf => cf.ByControlType(ControlType.Pane))
            .Where(e => e.AutomationId is { } id && id.StartsWith("Dock.Inspector.", StringComparison.Ordinal))
            .ToArray();
        Assert.Empty(fieldRows);
    }

    private static AutomationElement WaitForElement(Window window, string automationId, ControlType? controlType = null)
    {
        var cf = window.Automation.ConditionFactory;
        return Retry.WhileNull(
                () => window.FindFirstDescendant(controlType is null
                    ? cf.ByAutomationId(automationId)
                    : cf.ByAutomationId(automationId).And(cf.ByControlType(controlType.Value))),
                timeout: TimeSpan.FromSeconds(30),
                throwOnTimeout: true)
            .Result;
    }

    private static AutomationElement WaitForDescendant(AutomationElement parent, string automationId, ControlType? controlType = null)
    {
        var cf = parent.Automation.ConditionFactory;
        return Retry.WhileNull(
                () => parent.FindFirstDescendant(controlType is null
                    ? cf.ByAutomationId(automationId)
                    : cf.ByAutomationId(automationId).And(cf.ByControlType(controlType.Value))),
                timeout: TimeSpan.FromSeconds(15),
                throwOnTimeout: true)
            .Result;
    }

    private static AutomationElement WaitForHeader(AutomationElement inspector, string prefix)
    {
        AutomationElement? header = null;
        Retry.WhileNull(
            () =>
            {
                header = inspector
                    .FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
                    .FirstOrDefault(e => e.AutomationId is { } id && id.StartsWith(prefix, StringComparison.Ordinal));
                return header;
            },
            timeout: TimeSpan.FromSeconds(20),
            throwOnTimeout: true);

        return header!;
    }

    private static string GetHelpText(AutomationElement element)
        => element.Properties.HelpText.TryGetValue(out var helpText) ? helpText ?? string.Empty : string.Empty;

    private static string ReadText(AutomationElement element)
    {
        if (element is null)
        {
            return string.Empty;
        }

        if (element.Patterns.Text.IsSupported)
        {
            var text = element.Patterns.Text.Pattern.DocumentRange.GetText(int.MaxValue);
            return text.TrimEnd('\r', '\n');
        }

        return element.Name;
    }
}
