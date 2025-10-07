using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using YasGMP.Services.Interfaces;

namespace YasGMP.Views.Debug
{
    /// <summary>
    /// Debug log viewer page that lists collected app log files for download/inspection.
    /// </summary>
    public partial class LogViewerPage : ContentPage
    {
        /// <summary>
        /// Initializes a new instance of the LogViewerPage class.
        /// </summary>
        public LogViewerPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await Reload();
        }

        private async System.Threading.Tasks.Task Reload()
        {
            try
            {
                var dir = Path.Combine(FileSystem.AppDataDirectory, "logs");
                var file = Path.Combine(dir, $"{DateTime.UtcNow:yyyy-MM-dd}_diag.log");
                var lines = File.Exists(file) ? await System.IO.File.ReadAllLinesAsync(file) : Array.Empty<string>();
                var filter = SearchBox.Text;
                if (!string.IsNullOrWhiteSpace(filter))
                    lines = lines.Where(l => l.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToArray();
                LogList.ItemsSource = lines.Reverse();
            }
            catch { }
        }

        private async void OnReload(object sender, EventArgs e) => await Reload();
    }
}
