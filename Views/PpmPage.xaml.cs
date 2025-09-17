// ==============================================================================
//  File: Pages/PpmPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      GMP/Annex 11–compliant Preventive Maintenance Plans (PPM) page.
//      • Safe UI-thread dialogs via SafeNavigator/MainThread (fixes 0x8001010E)
//      • DI-friendly + parameterless constructors
//      • Reflection helpers to avoid brittle coupling with the ViewModel
//      • Robust connection-string resolution from App.AppConfig
// ==============================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Maui.Controls;
using YasGMP.Common;
using YasGMP.Services;
using YasGMP.ViewModels;

namespace YasGMP.Pages
{
    /// <summary>
    /// <b>PpmPage</b> — GMP/Annex 11–compliant <see cref="ContentPage"/> for Preventive Maintenance Plans (PPM).
    /// Provides a parameterless ctor (for XAML/Shell) and a DI-friendly ctor receiving <see cref="DatabaseService"/>.
    /// All dialogs and prompts are marshaled to the UI thread to avoid WinUI <c>0x8001010E</c> issues.
    /// </summary>
    public partial class PpmPage : ContentPage
    {
        /// <summary>
        /// The ViewModel instance used as this page's <see cref="BindableObject.BindingContext"/>.
        /// Kept as <see cref="object"/> to reduce compile-time coupling (reflection is used).
        /// </summary>
        private readonly PpmViewModel _vm;

        /// <summary>
        /// Creates the page from XAML/Shell without DI.
        /// Discovers the connection string from <c>App.AppConfig</c>.
        /// </summary>
        /// <exception cref="InvalidOperationException">When App or connection string is not available.</exception>
        public PpmPage(PpmViewModel viewModel)
        {
            if (viewModel is null) throw new ArgumentNullException(nameof(viewModel));

            InitializeComponent();

            _vm = viewModel;
            BindingContext = _vm;

            // Safe initial load
            Loaded += async (_, __) => await SafeLoadAsync();
        }

        /// <summary>DI-friendly constructor when you already have a <see cref="DatabaseService"/>.</summary>
        public PpmPage(DatabaseService db) : this(new PpmViewModel(db ?? throw new ArgumentNullException(nameof(db))))
        {
        }

        /// <summary>Parameterless ctor for Shell/XAML; resolves ViewModel via ServiceLocator.</summary>
        public PpmPage() : this(ServiceLocator.GetRequiredService<PpmViewModel>())
        {
        }

        #region === Startup / Data Load ===

        /// <summary>
        /// Loads plans through the ViewModel (if it exposes <c>LoadPpmPlansAsync</c>).
        /// </summary>
        private async Task SafeLoadAsync()
        {
            try
            {
                await InvokeIfExistsAsync("LoadPpmPlansAsync");
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", $"Neuspješno učitavanje PPM planova: {ex.Message}", "OK");
            }
        }

        #endregion

        #region === CRUD Handlers (Add / Save / Delete) ===

        /// <summary>
        /// Adds a new PPM plan using prompt dialogs, populates a new item, and persists via ViewModel methods if present.
        /// </summary>
        private async void OnAddPlanAsync(object? sender, EventArgs e)
        {
            try
            {
                // UI-thread prompts
                string? title = await PromptAsync("Novi PPM plan", "Naslov:");
                if (string.IsNullOrWhiteSpace(title)) return;

                string? desc = await PromptAsync("Novi PPM plan", "Opis:");
                string? freq = await PromptAsync("Novi PPM plan", "Frekvencija (npr. mjesečno):");
                string? due  = await PromptAsync("Novi PPM plan", "Sljedeći datum (yyyy-MM-dd):");

                DateTime? dueDate = null;
                if (!string.IsNullOrWhiteSpace(due) &&
                    DateTime.TryParseExact(due, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    dueDate = parsed;
                }

                var (plansList, elementType) = GetPlansListAndElementType();
                if (plansList is null || elementType is null)
                {
                    await Services.SafeNavigator.ShowAlertAsync("Greška", "PpmPlans kolekcija nije dostupna u ViewModelu.", "OK");
                    return;
                }

                var newItem = Activator.CreateInstance(elementType);
                if (newItem is null)
                {
                    await Services.SafeNavigator.ShowAlertAsync("Greška", "Nije moguće stvoriti instancu PPM plana.", "OK");
                    return;
                }

                SetIfExists(newItem, "Title", title);
                SetIfExists(newItem, "Description", desc);
                SetIfExists(newItem, "Frequency", freq);
                if (dueDate.HasValue) SetIfExists(newItem, "DueDate", dueDate.Value);
                SetIfExists(newItem, "Status", "draft");

                plansList.Add(newItem);
                SetVmPropertyValue("SelectedPlan", newItem);

                var saved = await InvokeIfExistsAsync("SaveSelectedPlanAsync");
                if (!saved)
                    await InvokeIfExistsAsync("SavePlanAsync", newItem);

                await Services.SafeNavigator.ShowAlertAsync("Info", "Novi plan je dodan.", "OK");
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", $"Dodavanje plana nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Saves the currently selected plan using either <c>SaveSelectedPlanAsync</c> or <c>SavePlanAsync</c>.
        /// </summary>
        private async void OnSavePlanAsync(object? sender, EventArgs e)
        {
            try
            {
                var selected = GetVmPropertyValue("SelectedPlan");
                if (selected is null)
                {
                    await Services.SafeNavigator.ShowAlertAsync("Obavijest", "Nema odabranog plana.", "OK");
                    return;
                }

                var saved = await InvokeIfExistsAsync("SaveSelectedPlanAsync");
                if (!saved)
                    await InvokeIfExistsAsync("SavePlanAsync", selected);

                await Services.SafeNavigator.ShowAlertAsync("Info", "Plan je spremljen.", "OK");
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", $"Spremanje nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Deletes the selected plan after confirmation using either <c>DeleteSelectedPlanAsync</c> or <c>DeletePlanAsync</c>.
        /// </summary>
        private async void OnDeletePlanAsync(object? sender, EventArgs e)
        {
            try
            {
                var selected = GetVmPropertyValue("SelectedPlan");
                if (selected is null)
                {
                    await Services.SafeNavigator.ShowAlertAsync("Obavijest", "Nema odabranog plana.", "OK");
                    return;
                }

                bool confirm = await Services.SafeNavigator.ConfirmAsync("Potvrda", "Želite li izbrisati odabrani plan?", "Da", "Ne");
                if (!confirm) return;

                var (plansList, _) = GetPlansListAndElementType();
                plansList?.Remove(selected);

                var deleted = await InvokeIfExistsAsync("DeleteSelectedPlanAsync");
                if (!deleted)
                    await InvokeIfExistsAsync("DeletePlanAsync", selected);

                SetVmPropertyValue("SelectedPlan", null);

                await Services.SafeNavigator.ShowAlertAsync("Info", "Plan je izbrisan.", "OK");
            }
            catch (Exception ex)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", $"Brisanje nije uspjelo: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Generates a work order from the currently selected plan (stub).
        /// </summary>
        private async void OnGenerateWorkOrderAsync(object? sender, EventArgs e)
        {
            await DisplayInfoStubAsync("Generiranje radnog naloga");
        }

        #endregion

        #region === Advanced Buttons (Clicked handlers) ===

        private async void OnAddAttachmentAsync(object? sender, EventArgs e)
        {
            if (GetVmPropertyValue("SelectedPlan") is null)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", "Nema odabranog PPM plana.", "OK");
                return;
            }

            var ran = await InvokeIfExistsAsync("AddAttachmentAsync");
            if (!ran) await DisplayInfoStubAsync("Upload privitka");
        }

        private async void OnShowAttachmentsAsync(object? sender, EventArgs e)
        {
            if (GetVmPropertyValue("SelectedPlan") is null)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", "Nema odabranog PPM plana.", "OK");
                return;
            }

            var ran = await InvokeIfExistsAsync("ShowAttachmentsAsync");
            if (!ran) await DisplayInfoStubAsync("Prikaz privitaka");
        }

        private async void OnApprovePlanAsync(object? sender, EventArgs e)
        {
            if (GetVmPropertyValue("SelectedPlan") is null)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", "Nema odabranog PPM plana.", "OK");
                return;
            }

            var ran = await InvokeIfExistsAsync("ApprovePlanAsync");
            if (!ran) await DisplayInfoStubAsync("Digitalni potpis");
        }

        private async void OnShowAuditAsync(object? sender, EventArgs e)
        {
            if (GetVmPropertyValue("SelectedPlan") is null)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", "Nema odabranog PPM plana.", "OK");
                return;
            }

            var ran = await InvokeIfExistsAsync("ShowAuditAsync");
            if (!ran) await DisplayInfoStubAsync("Prikaz audita");
        }

        private async void OnLinkCapaIncidentAsync(object? sender, EventArgs e)
        {
            if (GetVmPropertyValue("SelectedPlan") is null)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", "Nema odabranog PPM plana.", "OK");
                return;
            }

            var ran = await InvokeIfExistsAsync("LinkCapaIncidentAsync");
            if (!ran) await DisplayInfoStubAsync("Povezivanje s CAPA/incidentom");
        }

        private async void OnLinkIotSensorAsync(object? sender, EventArgs e)
        {
            if (GetVmPropertyValue("SelectedPlan") is null)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", "Nema odabranog PPM plana.", "OK");
                return;
            }

            var ran = await InvokeIfExistsAsync("LinkIotSensorAsync");
            if (!ran) await DisplayInfoStubAsync("Povezivanje s IoT senzorom");
        }

        private async void OnExportPlanAsPdfAsync(object? sender, EventArgs e)
        {
            if (GetVmPropertyValue("SelectedPlan") is null)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", "Nema odabranog PPM plana.", "OK");
                return;
            }

            var ran = await InvokeIfExistsAsync("ExportPlanAsPdfAsync");
            if (!ran) await DisplayInfoStubAsync("Eksport u PDF");
        }

        private async void OnScheduleNextReviewAsync(object? sender, EventArgs e)
        {
            if (GetVmPropertyValue("SelectedPlan") is null)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", "Nema odabranog PPM plana.", "OK");
                return;
            }

            var ran = await InvokeIfExistsAsync("ScheduleNextReviewAsync");
            if (!ran) await DisplayInfoStubAsync("Zakazivanje revizije");
        }

        private async void OnAssignToExternalContractorAsync(object? sender, EventArgs e)
        {
            if (GetVmPropertyValue("SelectedPlan") is null)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", "Nema odabranog PPM plana.", "OK");
                return;
            }

            var ran = await InvokeIfExistsAsync("AssignToExternalContractorAsync");
            if (!ran) await DisplayInfoStubAsync("Dodjela vanjskom izvođaču");
        }

        private async void OnAddInspectorCommentAsync(object? sender, EventArgs e)
        {
            if (GetVmPropertyValue("SelectedPlan") is null)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", "Nema odabranog PPM plana.", "OK");
                return;
            }

            var ran = await InvokeIfExistsAsync("AddInspectorCommentAsync");
            if (!ran) await DisplayInfoStubAsync("Komentar inspektora");
        }

        private async void OnAttachChecklistCertificateAsync(object? sender, EventArgs e)
        {
            if (GetVmPropertyValue("SelectedPlan") is null)
            {
                await Services.SafeNavigator.ShowAlertAsync("Greška", "Nema odabranog PPM plana.", "OK");
                return;
            }

            var ran = await InvokeIfExistsAsync("AttachChecklistCertificateAsync");
            if (!ran) await DisplayInfoStubAsync("Dodavanje checkliste/certifikata");
        }

        #endregion

        #region === Helpers (reflection + service resolution + UI prompts) ===

        /// <summary>
        /// Gets the ViewModel's <c>PpmPlans</c> collection (if exposed) and the element type.
        /// </summary>
        private (IList? Plans, Type? ElementType) GetPlansListAndElementType()
        {
            if (_vm is null)
                return (null, null);

            var property = GetPropertyInfo(_vm, "PpmPlans");
            if (property is null)
                return (null, null);

            if (property.GetValue(_vm) is not IList list)
                return (null, null);

            var elementType = ExtractElementType(property.PropertyType);

            return (list, elementType);
        }

        /// <summary>
        /// Reads a property value from the ViewModel via reflection.
        /// </summary>
        private object? GetVmPropertyValue(string propertyName)
        {
            if (_vm is null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            var property = GetPropertyInfo(_vm, propertyName);
            return property?.GetValue(_vm);
        }

        /// <summary>
        /// Sets a property on the ViewModel if it exists.
        /// </summary>
        private void SetVmPropertyValue(string propertyName, object? value)
        {
            if (_vm is null || string.IsNullOrWhiteSpace(propertyName))
                return;

            var property = GetPropertyInfo(_vm, propertyName);
            if (property is null || !property.CanWrite)
                return;

            try
            {
                property.SetValue(_vm, ConvertToPropertyType(value, property.PropertyType));
            }
            catch
            {
                // silent failure keeps the helper defensive as in the previous implementation
            }
        }

        /// <summary>
        /// Sets a property on a target object when such property exists.
        /// </summary>
        private static void SetIfExists(object target, string propertyName, object? value)
        {
            if (target is null || string.IsNullOrWhiteSpace(propertyName))
                return;

            var property = GetPropertyInfo(target, propertyName);
            if (property is null || !property.CanWrite)
                return;

            try
            {
                property.SetValue(target, ConvertToPropertyType(value, property.PropertyType));
            }
            catch
            {
                // ignore conversion issues – helper is best-effort
            }
        }

        /// <summary>
        /// Invokes an asynchronous method on the ViewModel if present.
        /// Returns <c>true</c> when a matching method existed and completed successfully.
        /// </summary>
        private async Task<bool> InvokeIfExistsAsync(string methodName, params object?[] args)
        {
            if (_vm is null || string.IsNullOrWhiteSpace(methodName))
                return false;

            var method = GetMethodInfo(_vm, methodName, args.Length);
            if (method is null)
                return false;

            var parameters = method.GetParameters();
            var invocationArgs = new object?[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                invocationArgs[i] = ConvertToPropertyType(args[i], parameters[i].ParameterType);
            }

            var result = method.Invoke(_vm, invocationArgs);
            if (result is null)
                return true;

            if (result is Task task)
            {
                await task.ConfigureAwait(false);

                if (task.GetType().IsGenericType)
                {
                    var resultProperty = task.GetType().GetRuntimeProperty("Result");
                    if (resultProperty?.GetValue(task) is bool boolResult)
                        return boolResult;
                }

                return true;
            }

            if (result is bool boolValue)
                return boolValue;

            return true;
        }

        /// <summary>
        /// Shows an informational stub alert for features that are not yet implemented.
        /// </summary>
        private static async Task DisplayInfoStubAsync(string featureName)
        {
            var title = string.IsNullOrWhiteSpace(featureName) ? "Info" : featureName;
            var message = string.IsNullOrWhiteSpace(featureName)
                ? "Funkcionalnost je trenutno u pripremi."
                : $"\u201E{featureName}\u201C funkcionalnost je trenutno u pripremi.";

            await Services.SafeNavigator.ShowAlertAsync(title, message, "OK").ConfigureAwait(false);
        }

        /// <summary>
        /// UI-thread safe prompt helper used for CRUD dialogs.
        /// </summary>
        private Task<string?> PromptAsync(string title, string message)
            => MainThread.InvokeOnMainThreadAsync(() => DisplayPromptAsync(title, message));

        private static PropertyInfo? GetPropertyInfo(object target, string propertyName)
        {
            if (target is null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            return target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static MethodInfo? GetMethodInfo(object target, string methodName, int argumentCount)
        {
            if (target is null || string.IsNullOrWhiteSpace(methodName))
                return null;

            return target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.Ordinal) &&
                                     m.GetParameters().Length == argumentCount);
        }

        private static Type? ExtractElementType(Type propertyType)
        {
            if (propertyType is null)
                return null;

            if (propertyType.IsArray)
                return propertyType.GetElementType();

            if (propertyType.IsGenericType)
            {
                var args = propertyType.GetGenericArguments();
                if (args.Length == 1)
                    return args[0];
            }

            if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
            {
                var enumerableInterface = propertyType.GetInterfaces()
                    .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

                return enumerableInterface?.GetGenericArguments().FirstOrDefault();
            }

            return null;
        }

        private static object? ConvertToPropertyType(object? value, Type propertyType)
        {
            if (value is null)
                return Nullable.GetUnderlyingType(propertyType) is not null || !propertyType.IsValueType
                    ? null
                    : Activator.CreateInstance(propertyType);

            var valueType = value.GetType();
            if (propertyType.IsAssignableFrom(valueType))
                return value;

            try
            {
                if (propertyType.IsEnum)
                {
                    if (value is string enumName)
                        return Enum.Parse(propertyType, enumName, ignoreCase: true);

                    return Enum.ToObject(propertyType, value);
                }

                var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
                return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch
            {
                return value;
            }
        }

        #endregion
    }
}
