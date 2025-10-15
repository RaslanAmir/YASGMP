using System.Threading.Tasks;

namespace YasGMP.Services.Ui
{
    /// <summary>
    /// Abstraction for prompting the user with alerts and confirmation dialogs on the UI thread.
    /// </summary>
    public interface IAlertService
    {
        /// <summary>
        /// Displays a one-button alert dialog or equivalent UI surface.
        /// </summary>
        /// <param name="title">Dialog title or heading.</param>
        /// <param name="message">Body content describing the alert.</param>
        /// <param name="cancel">Label for the acknowledgment button.</param>
        Task AlertAsync(string title, string message, string cancel = "OK");

        /// <summary>
        /// Displays a confirmation prompt and returns the operator's choice.
        /// </summary>
        /// <param name="title">Dialog title or heading.</param>
        /// <param name="message">Body content describing the decision.</param>
        /// <param name="accept">Label for the affirmative option.</param>
        /// <param name="cancel">Label for the negative option.</param>
        /// <returns><c>true</c> when the affirmative option is selected; otherwise <c>false</c>.</returns>
        Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
    }
}
