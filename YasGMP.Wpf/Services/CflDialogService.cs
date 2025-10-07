using System.Threading.Tasks;
using System.Windows;
using YasGMP.Wpf.Dialogs;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Default CFL dialog implementation that marshals requests onto the WPF dispatcher and presents the
/// modal picker with the shell's primary window as owner.
/// </summary>
/// <remarks>
/// <para>
/// Instances assume requests may arrive from background threads and therefore wrap dialog creation
/// in <see cref="Application.Current"/>'s dispatcher. The first available shell window is used as the
/// dialog owner so focus and modality align with Golden Arrow navigation expectations.
/// </para>
/// <para>
/// The service forwards the localized title and rows supplied via <see cref="CflRequest"/>; callers
/// are responsible for sourcing those values from the shared resource dictionaries to satisfy the
/// shell's localization requirements.
/// </para>
/// <para>
/// Confirmed and cancelled outcomes are pushed back through the returned <see cref="Task"/> so audit
/// aware consumers can write the resulting <see cref="CflResult"/> (or a null cancellation marker)
/// into their audit appenders alongside form mode transitions.
/// </para>
/// </remarks>
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
