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


        #endregion
    }
}
