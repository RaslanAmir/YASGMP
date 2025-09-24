using System.Threading;
using System.Threading.Tasks;

namespace YasGMP.Services
{
    /// <summary>
    /// Platform-agnostic dialog helpers for alerts, confirmations and modal editors.
    /// </summary>
    public interface IDialogService
    {
        Task ShowAlertAsync(string title, string message, string cancel);

        Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel);

        Task<string?> ShowActionSheetAsync(string title, string cancel, string? destruction, params string[] buttons);

        Task<T?> ShowDialogAsync<T>(string dialogId, object? parameter = null, CancellationToken cancellationToken = default);
    }
}
