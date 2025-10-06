using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels;

/// <summary>Inspector pane shown at the bottom of the shell displaying context details.</summary>
public partial class InspectorPaneViewModel : AnchorableViewModel
{
    private readonly ILocalizationService _localization;
    private string _modulePlaceholder;
    private string _recordPlaceholder;

    public InspectorPaneViewModel(ILocalizationService localization)
    {
        _localization = localization;
        Title = _localization.GetString("Dock.Inspector.Title");
        AutomationId = _localization.GetString("Dock.Inspector.AutomationId");
        ContentId = "YasGmp.Shell.Inspector";
        _modulePlaceholder = _localization.GetString("Dock.Inspector.ModuleTitle");
        _recordPlaceholder = _localization.GetString("Dock.Inspector.NoRecord");
        ModuleTitle = _modulePlaceholder;
        RecordTitle = _recordPlaceholder;
        Fields = new ObservableCollection<InspectorFieldViewModel>();
        _localization.LanguageChanged += OnLanguageChanged;
    }

    [ObservableProperty]
    private string _moduleTitle = "Module";

    [ObservableProperty]
    private string _recordTitle = "No record selected";

    public ObservableCollection<InspectorFieldViewModel> Fields { get; }

    public void Update(InspectorContext context)
    {
        ModuleTitle = string.IsNullOrWhiteSpace(context.ModuleTitle) ? _modulePlaceholder : context.ModuleTitle;
        RecordTitle = string.IsNullOrWhiteSpace(context.RecordTitle) ? _recordPlaceholder : context.RecordTitle;
        Fields.Clear();
        foreach (var field in context.Fields)
        {
            Fields.Add(new InspectorFieldViewModel(field.Label, field.Value));
        }
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        Title = _localization.GetString("Dock.Inspector.Title");
        AutomationId = _localization.GetString("Dock.Inspector.AutomationId");
        var previousModulePlaceholder = _modulePlaceholder;
        var previousRecordPlaceholder = _recordPlaceholder;
        _modulePlaceholder = _localization.GetString("Dock.Inspector.ModuleTitle");
        _recordPlaceholder = _localization.GetString("Dock.Inspector.NoRecord");

        if (string.IsNullOrWhiteSpace(ModuleTitle) || ModuleTitle == previousModulePlaceholder)
        {
            ModuleTitle = _modulePlaceholder;
        }

        if (string.IsNullOrWhiteSpace(RecordTitle) || RecordTitle == previousRecordPlaceholder)
        {
            RecordTitle = _recordPlaceholder;
        }
    }
}

public partial class InspectorFieldViewModel : ObservableObject
{
    public InspectorFieldViewModel(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }

    [ObservableProperty]
    private string _value;
}
