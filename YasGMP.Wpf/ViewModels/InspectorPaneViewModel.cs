using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels;

/// <summary>Inspector pane shown at the bottom of the shell displaying context details.</summary>
public partial class InspectorPaneViewModel : AnchorableViewModel
{
    private readonly ILocalizationService _localization;
    private string _modulePlaceholder = string.Empty;
    private string _moduleTemplate = string.Empty;
    private string _recordPlaceholder = string.Empty;
    private string _recordTemplate = string.Empty;
    private string _moduleAutomationNameTemplate = string.Empty;
    private string _moduleAutomationIdTemplate = string.Empty;
    private string _moduleAutomationTooltipTemplate = string.Empty;
    private string _moduleAutomationNameDefault = string.Empty;
    private string _moduleAutomationIdDefault = string.Empty;
    private string _moduleAutomationTooltipDefault = string.Empty;
    private string _recordAutomationNameTemplate = string.Empty;
    private string _recordAutomationIdTemplate = string.Empty;
    private string _recordAutomationTooltipTemplate = string.Empty;
    private string? _currentModuleContextValue;
    private string? _currentRecordContextValue;
    /// <summary>
    /// Initializes a new instance of the InspectorPaneViewModel class.
    /// </summary>

    public InspectorPaneViewModel(ILocalizationService localization)
    {
        _localization = localization;
        ContentId = "YasGmp.Shell.Inspector";
        LoadLocalizationResources();
        _currentModuleContextValue = null;
        _currentRecordContextValue = null;
        ApplyCurrentFormatting();
        Fields = new ObservableCollection<InspectorFieldViewModel>();
        _localization.LanguageChanged += OnLanguageChanged;
    }

    [ObservableProperty]
    private string _moduleTitle = "Module";

    [ObservableProperty]
    private string _recordTitle = "No record selected";

    [ObservableProperty]
    private string _moduleAutomationName = "Inspector module heading";

    [ObservableProperty]
    private string _moduleAutomationId = "Dock.Inspector.ModuleHeader";

    [ObservableProperty]
    private string _moduleAutomationTooltip = "Displays the currently active module.";

    [ObservableProperty]
    private string _recordAutomationName = "Inspector record heading";

    [ObservableProperty]
    private string _recordAutomationId = "Dock.Inspector.RecordHeader";

    [ObservableProperty]
    private string _recordAutomationTooltip = "Displays the currently selected record.";
    /// <summary>
    /// Gets or sets the fields.
    /// </summary>

    public ObservableCollection<InspectorFieldViewModel> Fields { get; }
    /// <summary>
    /// Executes the update operation.
    /// </summary>

    public void Update(InspectorContext context)
    {
        _currentModuleContextValue = string.IsNullOrWhiteSpace(context.ModuleTitle) ? null : context.ModuleTitle;
        _currentRecordContextValue = string.IsNullOrWhiteSpace(context.RecordTitle) ? null : context.RecordTitle;

        var moduleHasContext = !string.IsNullOrWhiteSpace(_currentModuleContextValue);
        var moduleText = moduleHasContext ? _currentModuleContextValue! : _modulePlaceholder;
        var recordText = string.IsNullOrWhiteSpace(_currentRecordContextValue) ? _recordPlaceholder : _currentRecordContextValue!;

        var presentation = BuildPresentation(moduleHasContext, moduleText, recordText);

        ModuleTitle = presentation.ModuleTitle;
        RecordTitle = presentation.RecordTitle;
        ModuleAutomationName = presentation.ModuleAutomationName;
        ModuleAutomationId = presentation.ModuleAutomationId;
        ModuleAutomationTooltip = presentation.ModuleAutomationTooltip;
        RecordAutomationName = presentation.RecordAutomationName;
        RecordAutomationId = presentation.RecordAutomationId;
        RecordAutomationTooltip = presentation.RecordAutomationTooltip;

        Fields.Clear();
        foreach (var field in context.Fields)
        {
            Fields.Add(CreateFieldViewModel(field, moduleText, recordText));
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        LoadLocalizationResources();
        ApplyCurrentFormatting();

        if (Fields.Count == 0)
        {
            return;
        }

        var moduleText = string.IsNullOrWhiteSpace(_currentModuleContextValue) ? _modulePlaceholder : _currentModuleContextValue!;
        var recordText = string.IsNullOrWhiteSpace(_currentRecordContextValue) ? _recordPlaceholder : _currentRecordContextValue!;

        foreach (var field in Fields)
        {
            var automation = BuildFieldAutomation(
                field.AutomationNameTemplate,
                field.AutomationIdTemplate,
                field.AutomationTooltipTemplate,
                field.Label,
                field.Value ?? string.Empty,
                moduleText,
                recordText);

            field.AutomationName = automation.AutomationName;
            field.AutomationId = automation.AutomationId;
            field.AutomationTooltip = automation.AutomationTooltip;
        }
    }

    private void LoadLocalizationResources()
    {
        Title = _localization.GetString("Dock.Inspector.Title");
        AutomationId = _localization.GetString("Dock.Inspector.AutomationId");
        _modulePlaceholder = _localization.GetString("Dock.Inspector.ModuleTitle");
        _moduleTemplate = _localization.GetString("Dock.Inspector.ModuleTitle.Template");
        _recordPlaceholder = _localization.GetString("Dock.Inspector.NoRecord");
        _recordTemplate = _localization.GetString("Dock.Inspector.RecordTitle.Template");
        _moduleAutomationNameDefault = _localization.GetString("Dock.Inspector.Module.AutomationName");
        _moduleAutomationIdDefault = _localization.GetString("Dock.Inspector.Module.AutomationId");
        _moduleAutomationTooltipDefault = _localization.GetString("Dock.Inspector.Module.ToolTip");
        _moduleAutomationNameTemplate = _localization.GetString("Dock.Inspector.Module.AutomationName.Template");
        _moduleAutomationIdTemplate = _localization.GetString("Dock.Inspector.Module.AutomationId.Template");
        _moduleAutomationTooltipTemplate = _localization.GetString("Dock.Inspector.Module.ToolTip.Template");
        _recordAutomationNameTemplate = _localization.GetString("Dock.Inspector.Record.AutomationName.Template");
        _recordAutomationIdTemplate = _localization.GetString("Dock.Inspector.Record.AutomationId.Template");
        _recordAutomationTooltipTemplate = _localization.GetString("Dock.Inspector.Record.ToolTip.Template");
    }

    private void ApplyCurrentFormatting()
    {
        var moduleHasContext = !string.IsNullOrWhiteSpace(_currentModuleContextValue);
        var moduleText = moduleHasContext ? _currentModuleContextValue! : _modulePlaceholder;
        var recordText = string.IsNullOrWhiteSpace(_currentRecordContextValue) ? _recordPlaceholder : _currentRecordContextValue!;

        var presentation = BuildPresentation(moduleHasContext, moduleText, recordText);

        ModuleTitle = presentation.ModuleTitle;
        RecordTitle = presentation.RecordTitle;
        ModuleAutomationName = presentation.ModuleAutomationName;
        ModuleAutomationId = presentation.ModuleAutomationId;
        ModuleAutomationTooltip = presentation.ModuleAutomationTooltip;
        RecordAutomationName = presentation.RecordAutomationName;
        RecordAutomationId = presentation.RecordAutomationId;
        RecordAutomationTooltip = presentation.RecordAutomationTooltip;
    }

    private (string ModuleTitle, string RecordTitle, string ModuleAutomationName, string ModuleAutomationId, string ModuleAutomationTooltip, string RecordAutomationName, string RecordAutomationId, string RecordAutomationTooltip) BuildPresentation(bool moduleHasContext, string moduleText, string recordText)
    {
        var moduleTitle = FormatString(_moduleTemplate, moduleText);
        var recordTitle = FormatString(_recordTemplate, recordText);

        string moduleAutomationName;
        string moduleAutomationId;
        string moduleAutomationTooltip;

        if (moduleHasContext)
        {
            moduleAutomationName = FormatString(_moduleAutomationNameTemplate, moduleText);
            moduleAutomationId = FormatString(_moduleAutomationIdTemplate, NormalizeAutomationToken(moduleText));
            moduleAutomationTooltip = FormatString(_moduleAutomationTooltipTemplate, moduleText);
        }
        else
        {
            moduleAutomationName = _moduleAutomationNameDefault;
            moduleAutomationId = _moduleAutomationIdDefault;
            moduleAutomationTooltip = _moduleAutomationTooltipDefault;
        }

        var recordAutomationName = FormatString(_recordAutomationNameTemplate, recordText);
        var recordAutomationId = FormatString(_recordAutomationIdTemplate, NormalizeAutomationToken(recordText));
        var recordAutomationTooltip = FormatString(_recordAutomationTooltipTemplate, recordText);

        return (
            moduleTitle,
            recordTitle,
            moduleAutomationName,
            moduleAutomationId,
            moduleAutomationTooltip,
            recordAutomationName,
            recordAutomationId,
            recordAutomationTooltip);
    }

    private InspectorFieldViewModel CreateFieldViewModel(InspectorField field, string moduleText, string recordText)
    {
        var label = field.Label ?? string.Empty;
        var value = field.Value ?? string.Empty;

        var automationNameTemplate = string.IsNullOrWhiteSpace(field.AutomationName)
            ? "{0} — {2} ({1})"
            : field.AutomationName;
        var automationIdTemplate = string.IsNullOrWhiteSpace(field.AutomationId)
            ? "Dock.Inspector.{4}.{5}.{6}"
            : field.AutomationId;
        var automationTooltipTemplate = string.IsNullOrWhiteSpace(field.AutomationTooltip)
            ? "{2} for {1} in {0}."
            : field.AutomationTooltip;

        var automation = BuildFieldAutomation(
            automationNameTemplate,
            automationIdTemplate,
            automationTooltipTemplate,
            label,
            value,
            moduleText,
            recordText);

        var fieldViewModel = new InspectorFieldViewModel(
            label,
            value,
            automationNameTemplate,
            automationIdTemplate,
            automationTooltipTemplate,
            automation.AutomationName,
            automation.AutomationId,
            automation.AutomationTooltip);

        fieldViewModel.ConfigureAutomationRecalculation(newValue =>
        {
            var automationUpdate = BuildFieldAutomation(
                automationNameTemplate,
                automationIdTemplate,
                automationTooltipTemplate,
                label,
                newValue ?? string.Empty,
                moduleText,
                recordText);

            return automationUpdate;
        });

        return fieldViewModel;
    }

    private (string AutomationName, string AutomationId, string AutomationTooltip) BuildFieldAutomation(
        string automationNameTemplate,
        string automationIdTemplate,
        string automationTooltipTemplate,
        string label,
        string value,
        string moduleText,
        string recordText)
    {
        var moduleToken = NormalizeAutomationToken(moduleText);
        var recordToken = NormalizeAutomationToken(recordText);
        var labelToken = NormalizeAutomationToken(label);

        var formatArgs = new object[]
        {
            moduleText,
            recordText,
            label,
            value,
            moduleToken,
            recordToken,
            labelToken,
        };

        var automationName = FormatString(automationNameTemplate, formatArgs);
        var automationId = FormatString(automationIdTemplate, formatArgs);
        var automationTooltip = FormatString(automationTooltipTemplate, formatArgs);

        return (automationName, automationId, automationTooltip);
    }

    private static string FormatString(string template, params object[] values)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return values.Length > 0 ? Convert.ToString(values[0], CultureInfo.CurrentCulture) ?? string.Empty : string.Empty;
        }

        return string.Format(CultureInfo.CurrentCulture, template, values);
    }

    private static string NormalizeAutomationToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "None";
        }

        var normalized = new string(value.Where(char.IsLetterOrDigit).ToArray());
        return string.IsNullOrWhiteSpace(normalized) ? "Value" : normalized;
    }
}
/// <summary>
/// Represents the Inspector Field View Model.
/// </summary>

public partial class InspectorFieldViewModel : ObservableObject
{
    private readonly string _automationNameTemplate;
    private readonly string _automationIdTemplate;
    private readonly string _automationTooltipTemplate;
    private Func<string, (string AutomationName, string AutomationId, string AutomationTooltip)>? _automationRecalculation;

    /// <summary>
    /// Initializes a new instance of the InspectorFieldViewModel class.
    /// </summary>
    public InspectorFieldViewModel(
        string label,
        string value,
        string automationNameTemplate,
        string automationIdTemplate,
        string automationTooltipTemplate,
        string automationName,
        string automationId,
        string automationTooltip)
    {
        Label = label;
        Value = value;
        _automationNameTemplate = automationNameTemplate ?? string.Empty;
        _automationIdTemplate = automationIdTemplate ?? string.Empty;
        _automationTooltipTemplate = automationTooltipTemplate ?? string.Empty;
        AutomationName = automationName;
        AutomationId = automationId;
        AutomationTooltip = automationTooltip;
    }

    /// <summary>
    /// Gets or sets the label.
    /// </summary>

    public string Label { get; }

    [ObservableProperty]
    private string _value;

    [ObservableProperty]
    private string _automationName = string.Empty;

    [ObservableProperty]
    private string _automationId = string.Empty;

    [ObservableProperty]
    private string _automationTooltip = string.Empty;

    internal string AutomationNameTemplate => _automationNameTemplate;

    internal string AutomationIdTemplate => _automationIdTemplate;

    internal string AutomationTooltipTemplate => _automationTooltipTemplate;

    internal void ConfigureAutomationRecalculation(Func<string, (string AutomationName, string AutomationId, string AutomationTooltip)> recalculation)
    {
        _automationRecalculation = recalculation ?? throw new ArgumentNullException(nameof(recalculation));
    }

    partial void OnValueChanged(string value)
    {
        if (_automationRecalculation is null)
        {
            return;
        }

        var automation = _automationRecalculation.Invoke(value ?? string.Empty);
        AutomationName = automation.AutomationName;
        AutomationId = automation.AutomationId;
        AutomationTooltip = automation.AutomationTooltip;
    }
}
