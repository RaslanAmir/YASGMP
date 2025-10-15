using YasGMP.Models.DTO;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Exposes authentication dialogs required by the WPF shell to gate access and perform
    /// sensitive re-confirmation flows.
    /// </summary>
    public interface IAuthenticationDialogService
    {
        /// <summary>
        /// Ensures an authenticated user exists, presenting the login dialog if necessary.
        /// </summary>
        /// <returns><c>true</c> when authentication succeeded; otherwise <c>false</c>.</returns>
        bool EnsureAuthenticated();

        /// <summary>
        /// Prompts the operator to re-enter credentials and choose a GMP reason.
        /// </summary>
        /// <returns>The captured <see cref="ReauthenticationResult"/> or <c>null</c> when cancelled.</returns>
        ReauthenticationResult? PromptReauthentication();
    }
}
