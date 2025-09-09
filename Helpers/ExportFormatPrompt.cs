using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace YasGMP.Helpers
{
    public static class ExportFormatPrompt
    {
        /// <summary>
        /// Shows a small action sheet to choose CSV, XLSX, or PDF.
        /// Defaults to "csv" if cancelled or UI not ready.
        /// </summary>
        public static async Task<string> PromptAsync()
        {
            try
            {
                var choice = await MainThread.InvokeOnMainThreadAsync(async () =>
                    await (Application.Current?.MainPage)?.DisplayActionSheet(
                        "Export format", "Cancel", null, "CSV", "XLSX", "PDF")!);

                if (string.IsNullOrWhiteSpace(choice) || choice.Equals("Cancel", StringComparison.OrdinalIgnoreCase))
                    return "csv";

                return choice.Equals("XLSX", StringComparison.OrdinalIgnoreCase) ? "xlsx"
                     : choice.Equals("PDF", StringComparison.OrdinalIgnoreCase) ? "pdf"
                     : "csv";
            }
            catch
            {
                return "csv";
            }
        }
    }
}

