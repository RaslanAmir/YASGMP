using System;
using System.Globalization;
using Xunit;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.Tests;

public sealed class InspectorPaneViewModelTests : IDisposable
{
    private readonly LocalizationService _localization = new();
    private readonly string _originalLanguage;

    public InspectorPaneViewModelTests()
    {
        _originalLanguage = _localization.CurrentLanguage;
        _localization.SetLanguage("en");
    }

    [Fact]
    public void Update_ComposesLocalizedAutomationMetadata()
    {
        var viewModel = new InspectorPaneViewModel(_localization);

        var moduleTitle = _localization.GetString("Module.Title.Dashboard");
        var recordKey = "WO-1001";
        var recordTitle = "Preventive maintenance";

        var fields = new[]
        {
            InspectorField.Create("Dashboard", moduleTitle, recordKey, recordTitle, "Status", "Open"),
        };

        viewModel.Update(new InspectorContext("Dashboard", moduleTitle, recordKey, recordTitle, fields));

        var moduleToken = AutomationIdSanitizer.Normalize(moduleTitle, "module");
        var recordToken = AutomationIdSanitizer.Normalize(recordTitle, "record");
        var labelToken = AutomationIdSanitizer.Normalize("Status", "field");

        var fieldAutomationIdTemplate = _localization.GetString("Dock.Inspector.Field.AutomationId.Template");
        var moduleAutomationIdTemplate = _localization.GetString("Dock.Inspector.Module.AutomationId.Template");
        Assert.Equal(
            string.Format(CultureInfo.InvariantCulture, moduleAutomationIdTemplate, moduleToken),
            viewModel.ModuleAutomationId);

        var moduleAutomationNameTemplate = _localization.GetString("Dock.Inspector.Module.AutomationName.Template");
        Assert.Equal(
            string.Format(CultureInfo.CurrentCulture, moduleAutomationNameTemplate, moduleTitle),
            viewModel.ModuleAutomationName);

        var recordAutomationIdTemplate = _localization.GetString("Dock.Inspector.Record.AutomationId.Template");
        Assert.Equal(
            string.Format(CultureInfo.InvariantCulture, recordAutomationIdTemplate, recordToken),
            viewModel.RecordAutomationId);

        var field = Assert.Single(viewModel.Fields);
        Assert.Equal(
            string.Format(
                CultureInfo.InvariantCulture,
                fieldAutomationIdTemplate,
                moduleTitle,
                field.Label,
                recordTitle,
                field.Value,
                moduleToken,
                recordToken,
                labelToken),
            field.AutomationId);

        var fieldAutomationNameTemplate = _localization.GetString("Dock.Inspector.Field.AutomationName.Template");
        Assert.Equal(
            string.Format(CultureInfo.CurrentCulture, fieldAutomationNameTemplate, moduleTitle, field.Label, recordTitle),
            field.AutomationName);

        var fieldAutomationTooltipTemplate = _localization.GetString("Dock.Inspector.Field.AutomationTooltip.Template");
        Assert.Equal(
            string.Format(CultureInfo.CurrentCulture, fieldAutomationTooltipTemplate, field.Label, recordTitle, moduleTitle),
            field.AutomationTooltip);

        _localization.SetLanguage("hr");

        moduleTitle = _localization.GetString("Module.Title.Dashboard");
        moduleToken = AutomationIdSanitizer.Normalize(moduleTitle, "module");
        recordToken = AutomationIdSanitizer.Normalize(recordTitle, "record");

        moduleAutomationIdTemplate = _localization.GetString("Dock.Inspector.Module.AutomationId.Template");
        Assert.Equal(
            string.Format(CultureInfo.InvariantCulture, moduleAutomationIdTemplate, moduleToken),
            viewModel.ModuleAutomationId);

        moduleAutomationNameTemplate = _localization.GetString("Dock.Inspector.Module.AutomationName.Template");
        Assert.Equal(
            string.Format(CultureInfo.CurrentCulture, moduleAutomationNameTemplate, moduleTitle),
            viewModel.ModuleAutomationName);

        recordAutomationIdTemplate = _localization.GetString("Dock.Inspector.Record.AutomationId.Template");
        Assert.Equal(
            string.Format(CultureInfo.InvariantCulture, recordAutomationIdTemplate, recordToken),
            viewModel.RecordAutomationId);

        field = Assert.Single(viewModel.Fields);
        Assert.Equal(
            string.Format(
                CultureInfo.InvariantCulture,
                fieldAutomationIdTemplate,
                moduleTitle,
                field.Label,
                recordTitle,
                field.Value,
                moduleToken,
                recordToken,
                labelToken),
            field.AutomationId);

        fieldAutomationNameTemplate = _localization.GetString("Dock.Inspector.Field.AutomationName.Template");
        Assert.Equal(
            string.Format(CultureInfo.CurrentCulture, fieldAutomationNameTemplate, moduleTitle, field.Label, recordTitle),
            field.AutomationName);

        fieldAutomationTooltipTemplate = _localization.GetString("Dock.Inspector.Field.AutomationTooltip.Template");
        Assert.Equal(
            string.Format(CultureInfo.CurrentCulture, fieldAutomationTooltipTemplate, field.Label, recordTitle, moduleTitle),
            field.AutomationTooltip);
    }

    [Fact]
    public void Update_WithPlainField_ComposesTemplatesFromLocalization()
    {
        var viewModel = new InspectorPaneViewModel(_localization);

        var moduleTitle = _localization.GetString("Module.Title.Dashboard");
        var recordKey = "WO-2001";
        var recordTitle = "Filter replacement";

        var fields = new[]
        {
            new InspectorField("Status", "Scheduled"),
        };

        viewModel.Update(new InspectorContext("Dashboard", moduleTitle, recordKey, recordTitle, fields));

        var moduleToken = AutomationIdSanitizer.Normalize(moduleTitle, "module");
        var recordToken = AutomationIdSanitizer.Normalize(recordTitle, "record");
        var labelToken = AutomationIdSanitizer.Normalize("Status", "field");

        var fieldAutomationNameTemplate = _localization.GetString("Dock.Inspector.Field.AutomationName.Template");
        var fieldAutomationIdTemplate = _localization.GetString("Dock.Inspector.Field.AutomationId.Template");
        var fieldAutomationTooltipTemplate = _localization.GetString("Dock.Inspector.Field.AutomationTooltip.Template");

        var field = Assert.Single(viewModel.Fields);
        Assert.Equal(
            string.Format(CultureInfo.CurrentCulture, fieldAutomationNameTemplate, moduleTitle, field.Label, recordTitle),
            field.AutomationName);
        Assert.Equal(
            string.Format(
                CultureInfo.InvariantCulture,
                fieldAutomationIdTemplate,
                moduleTitle,
                field.Label,
                recordTitle,
                field.Value,
                moduleToken,
                recordToken,
                labelToken),
            field.AutomationId);
        Assert.Equal(
            string.Format(CultureInfo.CurrentCulture, fieldAutomationTooltipTemplate, field.Label, recordTitle, moduleTitle),
            field.AutomationTooltip);
    }

    public void Dispose() => _localization.SetLanguage(_originalLanguage);
}
