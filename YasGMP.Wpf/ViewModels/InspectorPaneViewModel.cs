using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels;

/// <summary>Inspector pane shown at the bottom of the shell displaying context details.</summary>
public partial class InspectorPaneViewModel : AnchorableViewModel
{
    public InspectorPaneViewModel()
    {
        Title = "Inspector";
        ContentId = "YasGmp.Shell.Inspector";
        Fields = new ObservableCollection<InspectorFieldViewModel>();
    }

    [ObservableProperty]
    private string _moduleTitle = "Module";

    [ObservableProperty]
    private string _recordTitle = "No record selected";

    public ObservableCollection<InspectorFieldViewModel> Fields { get; }

    public void Update(InspectorContext context)
    {
        ModuleTitle = context.ModuleTitle;
        RecordTitle = context.RecordTitle;
        Fields.Clear();
        foreach (var field in context.Fields)
        {
            Fields.Add(new InspectorFieldViewModel(field.Label, field.Value));
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
