using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Represents a toolbar command in the SAP B1 style command strip.
/// </summary>
public partial class ModuleToolbarCommand : ObservableObject
{
    private readonly ILocalizationService? _localization;
    private readonly string _captionKey;
    private readonly string? _toolTipKey;
    private readonly string? _automationNameKey;
    private readonly string? _automationIdKey;

    public string CaptionKey => _captionKey;

    public string? ToolTipKey => _toolTipKey;

    public string? AutomationNameKey => _automationNameKey;

    public string? AutomationIdKey => _automationIdKey;

    public ModuleToolbarCommand(
        string captionKey,
        ICommand command,
        ILocalizationService? localization = null,
        string? toolTipKey = null,
        string? automationNameKey = null,
        string? automationIdKey = null,
        FormMode? associatedMode = null)
    {
        if (string.IsNullOrWhiteSpace(captionKey))
        {
            throw new ArgumentException("Caption key must be provided.", nameof(captionKey));
        }

        Command = command ?? throw new ArgumentNullException(nameof(command));
        _localization = localization;
        _captionKey = captionKey;
        _toolTipKey = toolTipKey;
        _automationNameKey = automationNameKey;
        _automationIdKey = automationIdKey;
        AssociatedMode = associatedMode;
        UpdateLocalizedValues();

        if (_localization is not null)
        {
            _localization.LanguageChanged += OnLanguageChanged;
        }
    }

    /// <summary>Display text rendered in the toolbar.</summary>
    [ObservableProperty]
    private string _caption = string.Empty;

    /// <summary>Tooltip displayed when hovering the toolbar button.</summary>
    [ObservableProperty]
    private string? _toolTip;

    /// <summary>Automation-friendly name surfaced to screen readers.</summary>
    [ObservableProperty]
    private string? _automationName;

    /// <summary>Automation identifier for UIA/FlaUI.</summary>
    [ObservableProperty]
    private string? _automationId;

    /// <summary>Gets the command executed when the toolbar button is clicked.</summary>
    public ICommand Command { get; }

    /// <summary>Optional form mode associated with the toggle.</summary>
    public FormMode? AssociatedMode { get; }

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _isChecked;

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        UpdateLocalizedValues();
    }

    private void UpdateLocalizedValues()
    {
        if (_localization is null)
        {
            Caption = _captionKey;
            ToolTip = _toolTipKey;
            AutomationName = _automationNameKey ?? _captionKey;
            AutomationId = _automationIdKey ?? _automationNameKey ?? _captionKey;
            return;
        }

        Caption = _localization.GetString(_captionKey);
        ToolTip = _toolTipKey is null ? null : _localization.GetString(_toolTipKey);
        AutomationName = _localization.GetString(_automationNameKey ?? _captionKey);
        AutomationId = _automationIdKey is null ? AutomationName : _localization.GetString(_automationIdKey);
    }
}
