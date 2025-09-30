using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Models.DTO;
using YasGMP.Models.Enums;

namespace YasGMP.Services.Interfaces
{
    /// <summary>
    /// <b>ICalibrationService</b> – Interface for robust GMP/ISO 17025 compliant calibration management services.
    /// <para>Defines contract for calibration CRUD, certificates, audit trail, IoT, AI integration, and compliance logic.</para>
    /// </summary>
    public interface ICalibrationService
    {
        /// <summary>
        /// Gets all calibrations.
        /// </summary>
        /// <returns>List of calibrations.</returns>
        Task<List<Calibration>> GetAllAsync();

        /// <summary>
        /// Gets calibration by its unique ID.
        /// </summary>
        Task<Calibration> GetByIdAsync(int id);

        /// <summary>
        /// Gets calibrations for a given component.
        /// </summary>
        Task<List<Calibration>> GetByComponentAsync(int componentId);

        /// <summary>
        /// Gets calibrations for a supplier.
        /// </summary>
        Task<List<Calibration>> GetBySupplierAsync(int supplierId);

        /// <summary>
        /// Gets calibrations within a date range.
        /// </summary>
        Task<List<Calibration>> GetByDateRangeAsync(DateTime from, DateTime to);

        /// <summary>
        /// Gets calibrations that are overdue.
        /// </summary>
        Task<List<Calibration>> GetOverdueCalibrationsAsync();

        /// <summary>
        /// Creates a new calibration record.
        /// </summary>
        Task CreateAsync(Calibration cal, int userId, SignatureMetadataDto? signatureMetadata = null);

        /// <summary>
        /// Updates an existing calibration record.
        /// </summary>
        Task UpdateAsync(Calibration cal, int userId, SignatureMetadataDto? signatureMetadata = null);

        /// <summary>
        /// Deletes a calibration by its ID.
        /// </summary>
        Task DeleteAsync(int calibrationId, int userId);

        /// <summary>
        /// Attaches a certificate file to a calibration.
        /// </summary>
        Task AttachCertificateAsync(int calibrationId, string certFilePath, int userId, SignatureMetadataDto? signatureMetadata = null);

        /// <summary>
        /// Revokes a certificate for a calibration.
        /// </summary>
        Task RevokeCertificateAsync(int calibrationId, string reason, int userId, SignatureMetadataDto? signatureMetadata = null);

        /// <summary>
        /// Marks a calibration as successful.
        /// </summary>
        Task MarkAsSuccessfulAsync(int calibrationId, int userId, SignatureMetadataDto? signatureMetadata = null);

        /// <summary>
        /// Validates if a calibration is still within its valid period.
        /// </summary>
        bool IsValid(Calibration cal);

        /// <summary>
        /// Predicts the failure risk for a calibration (stub for AI).
        /// </summary>
        Task<double> PredictCalibrationFailureRiskAsync(int calibrationId);

        /// <summary>
        /// Gets IoT sensor data for a calibration (stub for IoT).
        /// </summary>
        Task<string> GetIoTSensorDataAsync(int calibrationId);

        /// <summary>
        /// Triggers recalibration (stub for workflow extension).
        /// </summary>
        Task TriggerRecalibrationAsync(int calibrationId);

        /// <summary>
        /// Links a calibration to a CAPA record.
        /// </summary>
        Task LinkToCapaAsync(int calibrationId, int capaId);
    }
}
