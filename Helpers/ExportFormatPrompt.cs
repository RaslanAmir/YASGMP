using System;
using System.Threading.Tasks;
using YasGMP.Services;

namespace YasGMP.Helpers
{
    /// <summary>
    /// Represents the export format prompt value.
    /// </summary>
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
                var choice = await SafeNavigator.ActionSheetAsync(
                    "Export format",
                    "Cancel",
                    null,
                    "CSV",
                    "XLSX",
                    "PDF").ConfigureAwait(false);

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

