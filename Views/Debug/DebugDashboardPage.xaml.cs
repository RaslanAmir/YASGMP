using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using YasGMP.Diagnostics;
using YasGMP.Services.Interfaces;

namespace YasGMP.Views.Debug
{
    public partial class DebugDashboardPage : ContentPage
    {
        private readonly ITrace _trace;
        private readonly DiagnosticContext _ctx;
        private readonly SelfTestRunner _tests;
        private readonly DiagnosticsHub _hub;

        public DebugDashboardPage()
        {
            InitializeComponent();
            var sp = Application.Current?.Handler?.MauiContext?.Services;
            _trace = sp?.GetService<ITrace>() ?? throw new InvalidOperationException("Trace not available");
            _ctx   = sp?.GetService<DiagnosticContext>() ?? throw new InvalidOperationException("DiagCtx not available");
            _hub = sp?.GetService<DiagnosticsHub>() ?? throw new InvalidOperationException("DiagnosticsHub not available");
            var runner = sp?.GetService<SelfTestRunner>();
            if (runner is not null)
            {
                _tests = runner;
            }
            else
            {
                var db = sp?.GetService<Services.DatabaseService>() ?? throw new InvalidOperationException("DB not available");
                _tests = new SelfTestRunner(db, _trace, _ctx);
            }
            // No RBAC gate while app is in development; pages are visible for all users
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            SchemaHashLabel.Text = $"Schema hash: {_ctx.DbSchemaHash ?? "(unknown)"}";
            try { PrimaryDirLabel.Text = $"Primary: {_hub.PrimaryLogDir}"; } catch { }
            try { SecondaryDirLabel.Text = $"Secondary: {_hub.SecondaryLogDir}"; } catch { }
        }

        private async void OnRunTests(object sender, EventArgs e)
        {
            StatusLabel.Text = "Running tests...";
            await _tests.RunAll();
            StatusLabel.Text = "Tests completed.";
        }

        private async void OnViewLogs(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("routes/logviewer");
        }

        private async void OnHealth(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("routes/health");
        }

        private void OnFlushElastic(object sender, EventArgs e)
        {
            var ok = _hub.FlushElasticBuffer();
            StatusLabel.Text = ok ? "Elastic buffer flush queued." : "No Elastic sink or nothing to flush.";
        }
    }
}
