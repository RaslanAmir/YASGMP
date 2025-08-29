using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace YasGMP.Helpers
{
    /// <summary>
    /// Proširenja za Navigation – async otvaranje modalnih dijaloga i dohvat rezultata.
    /// </summary>
    public static class NavigationExtensions
    {
        /// <summary>
        /// Otvori modalni dialog kao Page i čekaj rezultat.
        /// </summary>
        /// <param name="navigation">Navigation objekt (Application.Current.MainPage.Navigation)</param>
        /// <param name="dialogPage">Page koji predstavlja dijalog</param>
        /// <returns>Rezultat dijaloga (true/false/nothing)</returns>
        public static async Task<bool?> ShowDialogAsync(this INavigation navigation, Page dialogPage)
        {
            var tcs = new TaskCompletionSource<bool?>();
            dialogPage.Disappearing += (s, e) =>
            {
                if (dialogPage.BindingContext is IDialogResult dialogResult)
                    tcs.TrySetResult(dialogResult.DialogResult);
                else
                    tcs.TrySetResult(null);
            };
            await navigation.PushModalAsync(dialogPage);
            return await tcs.Task;
        }
    }

    /// <summary>
    /// Interface za dialoge koji vraćaju rezultat (koristi se u BindingContextu dialoga).
    /// </summary>
    public interface IDialogResult
    {
        /// <summary>
        /// Rezultat dijaloga (npr. true za OK, false za Cancel, null za zatvaranje bez izbora)
        /// </summary>
        bool? DialogResult { get; }
    }
}
