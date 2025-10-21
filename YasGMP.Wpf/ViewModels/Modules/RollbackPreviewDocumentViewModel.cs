using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Models.DTO;
using YasGMP.Services;
using YasGMP.Wpf.Services;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Document view-model that previews before/after JSON payloads for an audit entry and allows requesting a rollback.
/// </summary>
public sealed partial class RollbackPreviewDocumentViewModel : DocumentViewModel
{
    private readonly DatabaseService _database;
    private readonly IShellInteractionService _shellInteraction;
    private readonly AuditEntryDto _audit;
    private readonly ILocalizationService _localization;
    private readonly AsyncRelayCommand _rollbackCommand;
    private readonly AsyncRelayCommand _closeCommand;
    private readonly string _readyStatus;
    private readonly string _missingDataStatus;
    private readonly string _successStatusFormat;
    private readonly string _failureStatusFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="RollbackPreviewDocumentViewModel"/> class.
    /// </summary>
    public RollbackPreviewDocumentViewModel(
        DatabaseService database,
        IShellInteractionService shellInteraction,
        ILocalizationService localization,
        AuditEntryDto audit)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _shellInteraction = shellInteraction ?? throw new ArgumentNullException(nameof(shellInteraction));
        _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));

        var title = _localization.GetString("Audit.Rollback.Document.Title");
        Title = string.IsNullOrWhiteSpace(title) ? "Rollback Preview" : title;
        AutomationId = "RollbackPreviewDocument";
        ContentId = $"YasGmp.Shell.Document.RollbackPreview.{CreateStableKey(audit)}";

        Header = BuildHeader(audit);
        Summary = BuildSummary(audit);
        EntityDisplay = BuildEntityDisplay(audit);
        UserDisplay = BuildUserDisplay(audit);
        TimestampDisplay = BuildTimestampDisplay(audit);
        SignatureHash = audit.SignatureHash ?? string.Empty;

        OldJson = NormalizeJson(audit.OldValue);
        NewJson = NormalizeJson(audit.NewValue);

        SignatureStatus = EvaluateSignature(out var brush);
        SignatureBrush = brush;
        CanRollback = DetermineCanRollback();

        _readyStatus = _localization.GetString("Audit.Rollback.Status.Ready") ?? "Review the JSON diff before requesting a rollback.";
        _missingDataStatus = _localization.GetString("Audit.Rollback.Status.MissingData") ?? "This audit entry does not expose the data required for a rollback.";
        _successStatusFormat = _localization.GetString("Audit.Rollback.Status.Success") ?? "Rollback requested for {0}.";
        _failureStatusFormat = _localization.GetString("Audit.Rollback.Status.Failure") ?? "Rollback failed: {0}";

        StatusMessage = _readyStatus;
        _shellInteraction.UpdateStatus(StatusMessage);

        _rollbackCommand = new AsyncRelayCommand(ExecuteRollbackAsync, CanExecuteRollback);
        _closeCommand = new AsyncRelayCommand(CloseAsync, () => !IsBusy);
    }
    /// <summary>Human-readable header describing the audit entry.</summary>
    [ObservableProperty]
    private string _header = string.Empty;

    /// <summary>Formatted summary including actor and timestamp.</summary>
    [ObservableProperty]
    private string _summary = string.Empty;

    /// <summary>Displays the entity name/id.</summary>
    [ObservableProperty]
    private string _entityDisplay = string.Empty;

    /// <summary>Displays the actor information.</summary>
    [ObservableProperty]
    private string _userDisplay = string.Empty;

    /// <summary>Displays the localized timestamp.</summary>
    [ObservableProperty]
    private string _timestampDisplay = string.Empty;

    /// <summary>Previous JSON payload.</summary>
    [ObservableProperty]
    private string _oldJson = "{}";

    /// <summary>New JSON payload.</summary>
    [ObservableProperty]
    private string _newJson = "{}";

    /// <summary>Human-readable signature validation result.</summary>
    [ObservableProperty]
    private string _signatureStatus = "Unknown";

    /// <summary>Brush used to render the signature indicator.</summary>
    [ObservableProperty]
    private Brush _signatureBrush = Brushes.Gray;

    /// <summary>Hex-encoded signature hash.</summary>
    [ObservableProperty]
    private string _signatureHash = string.Empty;

    /// <summary>Whether the rollback command can be executed.</summary>
    [ObservableProperty]
    private bool _canRollback;

    /// <summary>Busy flag toggled while issuing rollback requests.</summary>
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Status text surfaced below the JSON diff.</summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Command executed when the user confirms the rollback.</summary>
    public IAsyncRelayCommand RollbackCommand => _rollbackCommand;

    /// <summary>Command executed when the user closes the document.</summary>
    public IAsyncRelayCommand CloseCommand => _closeCommand;

    private bool CanExecuteRollback() => CanRollback && !IsBusy;

    private bool DetermineCanRollback()
        => !string.IsNullOrWhiteSpace(_audit.EntityName)
           && !string.IsNullOrWhiteSpace(_audit.EntityId)
           && !string.Equals(OldJson, "{}", StringComparison.Ordinal);

    private string EvaluateSignature(out Brush brush)
    {
        if (string.IsNullOrWhiteSpace(_audit.SignatureHash))
        {
            brush = Brushes.DarkGray;
            return _localization.GetString("Audit.Rollback.Signature.Unknown") ?? "Signature unavailable";
        }

        var normalizedData = BuildSignaturePayload(_audit);
        var expected = _audit.SignatureHash!;
        var valid = VerifySha256(normalizedData, expected);

        if (valid)
        {
            brush = Brushes.SeaGreen;
            return _localization.GetString("Audit.Rollback.Signature.Valid") ?? "Signature valid";
        }

        brush = Brushes.IndianRed;
        return _localization.GetString("Audit.Rollback.Signature.Invalid") ?? "Signature invalid";
    }
    private static string BuildSignaturePayload(AuditEntryDto audit)
    {
        var action = audit.Action ?? string.Empty;
        var note = audit.Note ?? string.Empty;
        var when = audit.ActionAt.ToString("O");
        return $"{action}|{note}|{when}";
    }

    private static bool VerifySha256(string data, string expectedHex)
    {
        if (string.IsNullOrWhiteSpace(expectedHex))
        {
            return false;
        }

        if (!TryHexToBytes(expectedHex.Trim(), out var expected))
        {
            return false;
        }

        using var sha = SHA256.Create();
        var actual = sha.ComputeHash(Encoding.UTF8.GetBytes(data ?? string.Empty));
        if (actual.Length != expected.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static bool TryHexToBytes(string hex, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        if (hex.Length % 2 != 0)
        {
            return false;
        }

        var buffer = new byte[hex.Length / 2];
        for (int i = 0; i < buffer.Length; i++)
        {
            if (!byte.TryParse(hex.AsSpan(i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out var value))
            {
                return false;
            }

            buffer[i] = value;
        }

        bytes = buffer;
        return true;
    }

    private async Task ExecuteRollbackAsync()
    {
        if (!DetermineCanRollback())
        {
            StatusMessage = _missingDataStatus;
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = _localization.GetString("Audit.Rollback.Status.Submitting") ?? "Submitting rollback request...";
            _shellInteraction.UpdateStatus(StatusMessage);

            await _database.RollbackEntityAsync(
                _audit.EntityName!,
                _audit.EntityId!,
                OldJson).ConfigureAwait(false);

            var success = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                _successStatusFormat,
                EntityDisplay);
            StatusMessage = success;
        }
        catch (Exception ex)
        {
            var failure = string.Format(
                System.Globalization.CultureInfo.CurrentCulture,
                _failureStatusFormat,
                ex.Message);
            StatusMessage = failure;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task CloseAsync()
    {
        _shellInteraction.CloseDocument(this);
        return Task.CompletedTask;
    }

    private static string NormalizeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "{}";
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch
        {
            return json;
        }
    }

    private static string CreateStableKey(AuditEntryDto audit)
    {
        if (audit.Id.HasValue)
        {
            return audit.Id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(BuildSignaturePayload(audit)));
        return Convert.ToBase64String(bytes).Replace('/', '_').Replace('+', '-');
    }

    private static string BuildHeader(AuditEntryDto audit)
    {
        var action = string.IsNullOrWhiteSpace(audit.Action) ? "AUDIT" : audit.Action!;
        return $"{action} • {BuildEntityDisplay(audit)}";
    }

    private static string BuildSummary(AuditEntryDto audit)
        => $"{BuildTimestampDisplay(audit)} • {BuildUserDisplay(audit)}";

    private static string BuildEntityDisplay(AuditEntryDto audit)
    {
        var entity = string.IsNullOrWhiteSpace(audit.EntityName) ? "system" : audit.EntityName!;
        if (!string.IsNullOrWhiteSpace(audit.EntityId))
        {
            entity += $" #{audit.EntityId}";
        }

        return entity;
    }

    private static string BuildUserDisplay(AuditEntryDto audit)
    {
        if (!string.IsNullOrWhiteSpace(audit.Username) && audit.UserId.HasValue)
        {
            return $"{audit.Username} (#{audit.UserId.Value})";
        }

        if (!string.IsNullOrWhiteSpace(audit.Username))
        {
            return audit.Username!;
        }

        return audit.UserId.HasValue
            ? $"User #{audit.UserId.Value}"
            : "System";
    }

    private static string BuildTimestampDisplay(AuditEntryDto audit)
        => audit.Timestamp.ToLocalTime().ToString("f", System.Globalization.CultureInfo.CurrentCulture);

    partial void OnIsBusyChanged(bool value)
    {
        _rollbackCommand.NotifyCanExecuteChanged();
        _closeCommand.NotifyCanExecuteChanged();
    }

    partial void OnCanRollbackChanged(bool value)
    {
        _rollbackCommand.NotifyCanExecuteChanged();
    }

    partial void OnStatusMessageChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _shellInteraction.UpdateStatus(value);
        }
    }
}
