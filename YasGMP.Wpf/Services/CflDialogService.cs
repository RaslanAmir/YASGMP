using System.Threading.Tasks;
using System.Windows;
using YasGMP.Wpf.Dialogs;

namespace YasGMP.Wpf.Services;

public sealed class CflDialogService : ICflDialogService
{
    public Task<CflResult?> ShowAsync(CflRequest request)
    {
        var tcs = new TaskCompletionSource<CflResult?>();
        Application.Current.Dispatcher.Invoke(() =>
        {
            var dialog = new CflDialog
            {
                Owner = Application.Current.Windows.Count > 0 ? Application.Current.Windows[0] : null,
                DataContext = CreateViewModel(request, tcs)
            };

            dialog.ShowDialog();
            if (!tcs.Task.IsCompleted)
            {
                tcs.TrySetResult(null);
            }
        });

        return tcs.Task;
    }

    private static CflDialogViewModel CreateViewModel(CflRequest request, TaskCompletionSource<CflResult?> tcs)
    {
        var vm = new CflDialogViewModel(request);
        vm.Confirmed += (_, result) =>
        {
            tcs.TrySetResult(result);
            CloseDialog();
        };
        return vm;

        void CloseDialog()
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window is CflDialog dlg && Equals(dlg.DataContext, vm))
                {
                    dlg.DialogResult = true;
                    dlg.Close();
                    break;
                }
            }
        }
    }
}
