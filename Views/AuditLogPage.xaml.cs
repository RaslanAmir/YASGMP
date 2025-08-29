// ==============================================================================
//  File: Views/AuditLogPage.xaml.cs
//  Project: YasGMP
//  Summary:
//      Code-behind for AuditLogPage. Wires the BindingContext via DI when present,
//      or constructs it with DatabaseService if available; otherwise uses default ctor.
//      Triggers the initial data load safely.
//  © 2025 YasGMP. All rights reserved.
// ==============================================================================
using System;
using Microsoft.Maui.Controls;
using YasGMP.Services;
using YasGMP.ViewModels;

namespace YasGMP.Views
{
    /// <summary>
    /// Audit Log dashboard page (filters + CSV/XLSX/PDF export).
    /// </summary>
    public partial class AuditLogPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuditLogPage"/> class.
        /// </summary>
        public AuditLogPage()
        {
            InitializeComponent();
            TryWireViewModel();
        }

        /// <summary>
        /// Attempts to resolve <see cref="AuditLogViewModel"/> via DI, then falls back to
        /// construction with <see cref="DatabaseService"/> if available, else default ctor.
        /// Triggers initial data load in a fire-and-forget safe manner.
        /// </summary>
        private async void TryWireViewModel()
        {
            try
            {
                var services = Application.Current?.Handler?.MauiContext?.Services;

                object? vmObj = services?.GetService(typeof(AuditLogViewModel));
                if (vmObj is AuditLogViewModel vmFromDi)
                {
                    BindingContext = vmFromDi;
                }
                else
                {
                    // Try to resolve DatabaseService, then inject into VM
                    var db = services?.GetService(typeof(DatabaseService)) as DatabaseService;
                    BindingContext = db != null ? new AuditLogViewModel(db) : new AuditLogViewModel();
                }

                if (BindingContext is AuditLogViewModel vm)
                {
                    await vm.LoadAsync().ConfigureAwait(false);
                }
            }
            catch
            {
                // Design-time safe: ignore DI / resolution errors
            }
        }
    }
}
