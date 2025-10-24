using System;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;
using YasGMP.Services.Interfaces;

namespace YasGMP.Wpf.Services
{
    /// <summary>
    /// Minimal adapter that fulfills ICalibrationAuditService by delegating to the shared AuditService.
    /// It maps CalibrationAudit payloads into the canonical audit writers used across the app.
    /// </summary>
    public sealed class CalibrationAuditAdapter : ICalibrationAuditService
    {
        private readonly AuditService _audit;

        public CalibrationAuditAdapter(AuditService audit)
        {
            _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        }

        public Task CreateAsync(CalibrationAudit audit)
        {
            if (audit == null) throw new ArgumentNullException(nameof(audit));
            var action = audit.Action.ToString();
            var details = string.IsNullOrWhiteSpace(audit.Details) ? string.Empty : audit.Details;
            return _audit.LogCalibrationAuditAsync(audit.CalibrationId, action, details);
        }
    }
}

