using System;
using System.Text.Json;
using Microsoft.Maui.Controls;
using YasGMP.Services.Interfaces;

namespace YasGMP.Views.Debug
{
    /// <summary>
    /// Debug page that surfaces live health probes and environment metadata.
    /// </summary>
    public partial class HealthPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the HealthPage class.
        /// </summary>
        public HealthPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RefreshAsync();
        }

        private async void OnRefresh(object sender, EventArgs e) => await RefreshAsync();

        private System.Threading.Tasks.Task RefreshAsync()
        {
            var health = Diagnostics.HealthReport.BuildBasic();
            HealthJson.Text = JsonSerializer.Serialize(health, new JsonSerializerOptions { WriteIndented = true });
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
