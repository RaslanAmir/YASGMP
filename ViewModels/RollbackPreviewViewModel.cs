using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using YasGMP.Models.DTO;
using YasGMP.Services;

namespace YasGMP.ViewModels
{
    /// <summary>
    /// <b>RollbackPreviewViewModel</b> – ViewModel for the Rollback Preview modal/dialog.
    /// <para>
    /// • Presents before/after JSON snapshots for an audited change.<br/>
    /// • Verifies a digital signature (SHA-256) for forensic traceability, using constant-time comparison.<br/>
    /// • Allows enqueuing a rollback request through <see cref="DatabaseService"/> with UI-thread safe notifications.
    /// </para>
    /// </summary>
    public sealed class RollbackPreviewViewModel : ObservableObject
    {
        private readonly DatabaseService _db;

        /// <summary>
        /// The selected audit entry used to populate the preview and drive rollback.
        /// Never <see langword="null"/>.
        /// </summary>
        public AuditEntryDto SelectedAudit { get; }

        // ===== Backing fields (explicit properties to remain AOT/WinRT friendly) =====

        private string _oldJson = "{}";
        /// <summary>
        /// JSON representation of the entity <b>before</b> the audited change.
        /// </summary>
        public string OldJson
        {
            get => _oldJson;
            set => SetProperty(ref _oldJson, value);
        }

        private string _newJson = "{}";
        /// <summary>
        /// JSON representation of the entity <b>after</b> the audited change.
        /// </summary>
        public string NewJson
        {
            get => _newJson;
            set => SetProperty(ref _newJson, value);
        }

        private string _signatureStatus = "Unknown";
        /// <summary>
        /// Status text of the digital signature validation (e.g., <c>Valid</c>/<c>Invalid</c>).
        /// </summary>
        public string SignatureStatus
        {
            get => _signatureStatus;
            set => SetProperty(ref _signatureStatus, value);
        }

        private string _signatureColor = "Gray";
        /// <summary>
        /// UI color corresponding to <see cref="SignatureStatus"/> (e.g., <c>Green</c> or <c>Red</c>).
        /// </summary>
        public string SignatureColor
        {
            get => _signatureColor;
            set => SetProperty(ref _signatureColor, value);
        }

        private bool _canRollback;
        /// <summary>
        /// Indicates whether rollback is possible for the displayed audit entry
        /// (requires a non-empty <see cref="OldJson"/> and resolvable entity identity).
        /// </summary>
        public bool CanRollback
        {
            get => _canRollback;
            set => SetProperty(ref _canRollback, value);
        }

        /// <summary>
        /// Command that attempts to enqueue a rollback for the selected audit entry.
        /// </summary>
        public ICommand RollbackCommand { get; }

        /// <summary>
        /// Command that closes the modal/dialog (if the hosting view uses modal navigation).
        /// </summary>
        public ICommand CloseCommand { get; }

        /// <summary>
        /// Creates a new <see cref="RollbackPreviewViewModel"/>.
        /// </summary>
        /// <param name="db">Database service used to perform rollback operations.</param>
        /// <param name="audit">Audit entry to preview and potentially roll back.</param>
        /// <exception cref="ArgumentNullException">If any parameter is <see langword="null"/>.</exception>
        public RollbackPreviewViewModel(DatabaseService db, AuditEntryDto audit)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            SelectedAudit = audit ?? throw new ArgumentNullException(nameof(audit));

            RollbackCommand = new AsyncRelayCommand(ExecuteRollback);
            CloseCommand    = new AsyncRelayCommand(CloseModalAsync);

            LoadAuditData();
            ValidateSignature(SelectedAudit);
        }

        /// <summary>
        /// Loads the old/new JSON snapshots from <see cref="SelectedAudit"/>.
        /// </summary>
        private void LoadAuditData()
        {
            OldJson = string.IsNullOrWhiteSpace(SelectedAudit.OldValue) ? "{}" : SelectedAudit.OldValue!;
            NewJson = string.IsNullOrWhiteSpace(SelectedAudit.NewValue) ? "{}" : SelectedAudit.NewValue!;
            CanRollback = !string.Equals(OldJson, "{}", StringComparison.Ordinal);
        }

        /// <summary>
        /// Validates the digital signature/hash of the audit entry using SHA-256.
        /// Uses a constant-time comparison to mitigate timing attacks.
        /// </summary>
        /// <param name="audit">The audit entry to validate.</param>
        private void ValidateSignature(AuditEntryDto audit)
        {
            // Compose deterministic data string for hashing (ISO 8601).
            string action = audit.Action ?? string.Empty;
            string note   = audit.Note   ?? string.Empty;
            string when   = audit.ActionAt.ToString("O"); // non-nullable DateTime

            string data = $"{action}|{note}|{when}";
            bool valid = VerifySha256ConstantTime(data, audit.SignatureHash);

            SignatureStatus = valid ? "Valid" : "Invalid";
            SignatureColor  = valid ? "Green" : "Red";
        }

        /// <summary>
        /// Verifies SHA-256 of <paramref name="data"/> against a hex-encoded <paramref name="expectedHexHash"/> using constant-time equality.
        /// </summary>
        private static bool VerifySha256ConstantTime(string data, string? expectedHexHash)
        {
            if (string.IsNullOrWhiteSpace(expectedHexHash))
                return false;

            if (!TryHexToBytes(expectedHexHash.Trim(), out var expected))
                return false;

            using var sha = SHA256.Create();
            var actual = sha.ComputeHash(Encoding.UTF8.GetBytes(data ?? string.Empty));

            // Normalize length (if expected hash length is wrong, fail fast).
            if (expected.Length != actual.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }

        /// <summary>
        /// Parses a hex string into bytes. Returns <see langword="false"/> on invalid input.
        /// </summary>
        private static bool TryHexToBytes(string hex, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            if (hex.Length % 2 != 0) return false;

            var buffer = new byte[hex.Length / 2];
            for (int i = 0; i < buffer.Length; i++)
            {
                if (!byte.TryParse(hex.AsSpan(i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out var b))
                    return false;
                buffer[i] = b;
            }
            bytes = buffer;
            return true;
        }

        /// <summary>
        /// Attempts to enqueue a rollback request for the current audit entry.
        /// All user prompts are executed via <see cref="SafeNavigator"/> to ensure UI-thread access.
        /// </summary>
        private async Task ExecuteRollback()
        {
            // Guard: ensure we have the entity identity and a previous state to roll back to.
            if (string.IsNullOrWhiteSpace(SelectedAudit.EntityName) || string.IsNullOrWhiteSpace(SelectedAudit.EntityId) || !CanRollback)
            {
                await SafeNavigator.ShowAlertAsync("Rollback", "Ovaj zapis nedostaje potrebne podatke (entitet/ID ili prethodno stanje) pa se ne može vratiti.", "OK");
                return;
            }

            // Optional confirmation
            var confirm = await SafeNavigator.ConfirmAsync(
                "Potvrda", $"Vratiti entitet '{SelectedAudit.EntityName}' (ID={SelectedAudit.EntityId}) na prethodno stanje?",
                "Da", "Ne");
            if (!confirm) return;

            try
            {
                await _db.RollbackEntityAsync(
                    entityName: SelectedAudit.EntityName,
                    entityId: SelectedAudit.EntityId,
                    oldJson: OldJson).ConfigureAwait(false);

                await SafeNavigator.ShowAlertAsync("Rollback", "Entitet je uspješno vraćen na prethodno stanje.", "OK");
            }
            catch (Exception ex)
            {
                await SafeNavigator.ShowAlertAsync("Rollback greška", ex.Message, "OK");
            }
        }

        /// <summary>
        /// Closes the modal/dialog if a modal navigation stack is available.
        /// Executed on the UI thread to avoid WinUI dialog threading issues.
        /// </summary>
        private async Task CloseModalAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var nav = Application.Current?.MainPage?.Navigation;
                if (nav != null && nav.ModalStack.Count > 0)
                    await nav.PopModalAsync();
            });
        }
    }
}
