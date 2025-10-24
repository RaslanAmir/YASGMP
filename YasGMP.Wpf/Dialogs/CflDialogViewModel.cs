using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Wpf.Services;
using YasGMP.Wpf.Helpers;

namespace YasGMP.Wpf.Dialogs;

public partial class CflDialogViewModel : ObservableObject
{
    public CflDialogViewModel(CflRequest request)
    {
        Title = request.Title;
        Items = new ObservableCollection<CflItem>(request.Items);
        ConfirmCommand = new RelayCommand(Confirm, () => SelectedItem is not null);
    }

    public string Title { get; }

    public ObservableCollection<CflItem> Items { get; }

    public RelayCommand ConfirmCommand { get; }

    [ObservableProperty]
    private CflItem? _selectedItem;

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
        UiCommandHelper.NotifyCanExecuteOnUi(ConfirmCommand);
    }
}

