using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;


namespace YasGMP.Services.Ui
{
public interface IAlertService
{
Task AlertAsync(string title, string message, string cancel = "OK");
Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel");
}


public sealed class AlertService : IAlertService
{
public async Task AlertAsync(string title, string message, string cancel = "OK")
{
await MainThread.InvokeOnMainThreadAsync(async () =>
{
var page = Application.Current?.MainPage;
if (page != null)
await page.DisplayAlert(title, message, cancel);
});
}


public async Task<bool> ConfirmAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
{
return await MainThread.InvokeOnMainThreadAsync(async () =>
{
var page = Application.Current?.MainPage;
if (page == null) return false;
return await page.DisplayAlert(title, message, accept, cancel);
});
}
}
}