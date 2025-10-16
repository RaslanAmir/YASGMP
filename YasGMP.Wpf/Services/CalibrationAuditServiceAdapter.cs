using System;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Bridges WPF calibration workflows to the shared AppCore audit infrastructure so
/// calibration actions produce the same immutable trail as the MAUI client.
/// </summary>
public sealed class CalibrationAuditServiceAdapter : ICalibrationAuditService
{
    private readonly AuditService _auditService;

    public CalibrationAuditServiceAdapter(AuditService auditService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    /// <inheritdoc />
    public Task CreateAsync(CalibrationAudit audit)
    {
        ArgumentNullException.ThrowIfNull(audit);

        var action = audit.Action.ToString();
        var details = string.IsNullOrWhiteSpace(audit.Details)
            ? $"Calibration {audit.CalibrationId} action {audit.Action} by user {audit.UserId}"
            : audit.Details;

        if (!string.IsNullOrWhiteSpace(audit.NewValue) || !string.IsNullOrWhiteSpace(audit.OldValue))
        {
            details += $" | old={audit.OldValue}; new={audit.NewValue}";
        }

        if (!string.IsNullOrWhiteSpace(audit.Note))
        {
            details += $" | note={audit.Note}";
        }

        return _auditService.LogCalibrationAuditAsync(audit.CalibrationId, action, details);
    }
}
