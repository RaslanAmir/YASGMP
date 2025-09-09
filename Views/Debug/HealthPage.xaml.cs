using System;
using System.Text.Json;
using Microsoft.Maui.Controls;
using YasGMP.Services.Interfaces;

namespace YasGMP.Views.Debug
{
    public partial class HealthPage : ContentPage
    {
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
