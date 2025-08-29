using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services.Interfaces;

namespace YasGMP.Services
{
    /// <summary>
    /// Ultra Mega Robust GMP-compliant audit service for Supplier actions.
    /// Provides async audit record creation with digital signature, retry logic, and extensibility hooks.
    /// Fully documented with XML comments for IntelliSense and maintainability.
    /// </summary>
    public class SupplierAuditService : ISupplierAuditService
    {
        private readonly DatabaseService _db;
        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMilliseconds = 200;

        /// <summary>
        /// Initializes a new instance of the <see cref="SupplierAuditService"/> class.
        /// </summary>
        /// <param name="databaseService">Injected database service for DB operations.</param>
        public SupplierAuditService(DatabaseService databaseService)
        {
            _db = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        }

        /// <summary>
        /// Creates an audit log entry asynchronously for a Supplier action.
        /// Generates a digital signature for integrity and supports retry on transient failures.
        /// </summary>
        /// <param name="audit">SupplierAudit model containing audit details.</param>
        /// <returns>Task representing async operation completion.</returns>
        /// <exception cref="ArgumentNullException">Thrown when audit argument is null.</exception>
        public async Task CreateAsync(SupplierAudit audit)
        {
            if (audit == null)
                throw new ArgumentNullException(nameof(audit));

            // Ensure digital signature is present
            if (string.IsNullOrEmpty(audit.DigitalSignature))
                audit.DigitalSignature = GenerateDigitalSignature(audit);

            int attempt = 0;

            while (true)
            {
                try
                {
                    // Build SQL for inserting audit record
                    string sql = @"
                        INSERT INTO supplier_audit
                        (supplier_id, user_id, action, details, changed_at, source_ip, device_info, digital_signature)
                        VALUES
                        (@supplierId, @userId, @action, @details, @changedAt, @sourceIp, @deviceInfo, @digitalSignature)";

                    // Prepare parameters array for DB call
                    var parameters = new[]
                    {
                        new MySqlConnector.MySqlParameter("@supplierId", audit.SupplierId),
                        new MySqlConnector.MySqlParameter("@userId", audit.UserId),
                        new MySqlConnector.MySqlParameter("@action", audit.Action),
                        new MySqlConnector.MySqlParameter("@details", audit.Details ?? string.Empty),
                        new MySqlConnector.MySqlParameter("@changedAt", audit.ChangedAt),
                        new MySqlConnector.MySqlParameter("@sourceIp", audit.SourceIp ?? string.Empty),
                        new MySqlConnector.MySqlParameter("@deviceInfo", audit.DeviceInfo ?? string.Empty),
                        new MySqlConnector.MySqlParameter("@digitalSignature", audit.DigitalSignature)
                    };

                    // Execute DB insert operation
                    await _db.ExecuteNonQueryAsync(sql, parameters);

                    // Optional: Hook to send notifications or trigger events here

                    break; // Success, exit retry loop
                }
                catch (Exception ex) when (IsTransient(ex) && attempt < MaxRetryAttempts)
                {
                    attempt++;
                    await Task.Delay(RetryDelayMilliseconds * attempt);
                }
                catch
                {
                    // Log or rethrow fatal exceptions as appropriate
                    throw;
                }
            }
        }

        /// <summary>
        /// Generates a digital signature (SHA256 hash) of key audit fields for integrity verification.
        /// </summary>
        /// <param name="audit">Audit entry to sign.</param>
        /// <returns>Base64 encoded digital signature string.</returns>
        private string GenerateDigitalSignature(SupplierAudit audit)
        {
            string rawData = $"{audit.SupplierId}|{audit.UserId}|{audit.Action}|{audit.ChangedAt:O}|{audit.Details}";
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Determines whether an exception is transient (e.g., network timeout, deadlock) and can be retried.
        /// </summary>
        /// <param name="ex">Exception to evaluate.</param>
        /// <returns>True if transient; otherwise, false.</returns>
        private bool IsTransient(Exception ex)
        {
            // Here you could add logic to detect transient DB exceptions
            // For MySQL, you can check for specific error codes (e.g., deadlock = 1213)
            // Simplified for demo: treat all MySqlException as transient
            return ex is MySqlConnector.MySqlException;
        }
    }
}
