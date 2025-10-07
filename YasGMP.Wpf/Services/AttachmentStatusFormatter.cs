using System;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Shared helper that produces consistent status messages for attachment uploads.
/// </summary>
public static class AttachmentStatusFormatter
{
    /// <summary>
    /// Executes the format operation.
    /// </summary>
    public static string Format(int processed, int deduplicated)
    {
        if (processed <= 0)
        {
            return "No attachments were processed.";
        }

        if (processed == 1)
        {
            return deduplicated == 1
                ? "Linked existing attachment (deduplicated)."
                : "Attachment uploaded successfully.";
        }

        if (deduplicated == 0)
        {
            return $"Uploaded {processed} attachments successfully.";
        }

        if (deduplicated == processed)
        {
            return $"Linked {processed} existing attachments (deduplicated).";
        }

        var newCount = processed - deduplicated;
        return $"Processed {processed} attachments ({newCount} new, {deduplicated} deduplicated).";
    }
}
