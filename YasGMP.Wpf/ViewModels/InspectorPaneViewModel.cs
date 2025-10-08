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

        ApplyFormatting(moduleHasContext, moduleText, recordText);
        Fields.Clear();
        foreach (var field in context.Fields)
        {
            Fields.Add(new InspectorFieldViewModel(
                field.Label,
                field.Value,
                field.AutomationName,
                field.AutomationId,
                field.AutomationTooltip));
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        LoadLocalizationResources();
        ModuleAutomationName = _moduleAutomationNameDefault;
        ModuleAutomationId = _moduleAutomationIdDefault;
        ModuleAutomationTooltip = _moduleAutomationTooltipDefault;
        RecordAutomationName = FormatString(_recordAutomationNameTemplate, _recordPlaceholder);
        RecordAutomationId = FormatString(_recordAutomationIdTemplate, NormalizeAutomationToken(_recordPlaceholder));
        RecordAutomationTooltip = FormatString(_recordAutomationTooltipTemplate, _recordPlaceholder);
        ApplyCurrentFormatting();
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

        ApplyFormatting(moduleHasContext, moduleText, recordText);
    }

    private void ApplyFormatting(bool moduleHasContext, string moduleText, string recordText)
    {
        ModuleTitle = FormatString(_moduleTemplate, moduleText);
        RecordTitle = FormatString(_recordTemplate, recordText);

        if (moduleHasContext)
        {
            ModuleAutomationName = FormatString(_moduleAutomationNameTemplate, moduleText);
            ModuleAutomationId = FormatString(_moduleAutomationIdTemplate, NormalizeAutomationToken(moduleText));
            ModuleAutomationTooltip = FormatString(_moduleAutomationTooltipTemplate, moduleText);
        }
        else
        {
            ModuleAutomationName = _moduleAutomationNameDefault;
            ModuleAutomationId = _moduleAutomationIdDefault;
            ModuleAutomationTooltip = _moduleAutomationTooltipDefault;
        }

        RecordAutomationName = FormatString(_recordAutomationNameTemplate, recordText);
        RecordAutomationId = FormatString(_recordAutomationIdTemplate, NormalizeAutomationToken(recordText));
        RecordAutomationTooltip = FormatString(_recordAutomationTooltipTemplate, recordText);
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
    /// <summary>
    /// Initializes a new instance of the InspectorFieldViewModel class.
    /// </summary>
    public InspectorFieldViewModel(
        string label,
        string value,
        string automationName,
        string automationId,
        string automationTooltip)
    {
        Label = label;
        Value = value;
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
}
