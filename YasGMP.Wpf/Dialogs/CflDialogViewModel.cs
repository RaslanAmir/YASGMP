using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.Dialogs;
/// <summary>
/// Represents the Cfl Dialog View Model.
/// </summary>

public partial class CflDialogViewModel : ObservableObject
{
    /// <summary>
    /// Initializes a new instance of the CflDialogViewModel class.
    /// </summary>
    public CflDialogViewModel(CflRequest request)
    {
        Title = request.Title;
        Items = new ObservableCollection<CflItem>(request.Items);
        ConfirmCommand = new RelayCommand(Confirm, () => SelectedItem is not null);
    }
    /// <summary>
    /// Gets or sets the title.
    /// </summary>

    public string Title { get; }
    /// <summary>
    /// Gets or sets the items.
    /// </summary>

    public ObservableCollection<CflItem> Items { get; }
    /// <summary>
    /// Gets or sets the confirm command.
    /// </summary>

    public RelayCommand ConfirmCommand { get; }

    [ObservableProperty]
    private CflItem? _selectedItem;
    /// <summary>
    /// Occurs when event handler is raised.
    /// </summary>

    public event EventHandler<CflResult>? Confirmed;

    private void Confirm()
    {
        if (SelectedItem is null)
        {
            return;
        }

        Confirmed?.Invoke(this, new CflResult(SelectedItem));
    }

    partial void OnSelectedItemChanged(CflItem? value)
    {
        ConfirmCommand.NotifyCanExecuteChanged();
    }
}
