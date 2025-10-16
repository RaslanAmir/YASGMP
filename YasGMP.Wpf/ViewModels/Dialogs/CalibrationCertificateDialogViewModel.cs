using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YasGMP.Services;

namespace YasGMP.Wpf.ViewModels.Dialogs;

/// <summary>
/// Immutable snapshot describing calibration certificate metadata that can be surfaced in the
/// editor and persisted alongside calibration records.
/// </summary>
/// <param name="DocumentName">Human friendly document name or descriptor.</param>
/// <param name="CertificateNumber">Accredited certificate identifier, if available.</param>
/// <param name="Issuer">Laboratory or authority that issued the certificate.</param>
/// <param name="IssuedOn">Date the certificate was issued.</param>
/// <param name="ExpiresOn">Date the certificate expires.</param>
/// <param name="Notes">Free-form notes for the certificate.</param>
/// <param name="AttachmentId">Existing attachment identifier if the certificate file has been uploaded.</param>
/// <param name="FileName">File name associated with the certificate.</param>
/// <param name="FileSize">File size in bytes.</param>
/// <param name="ContentType">Content type captured during upload.</param>
/// <param name="Sha256">SHA-256 hash of the uploaded file, if known.</param>
public sealed record CalibrationCertificateSnapshot(
    string DocumentName,
    string? CertificateNumber = null,
    string? Issuer = null,
    DateTime? IssuedOn = null,
    DateTime? ExpiresOn = null,
    string? Notes = null,
    int? AttachmentId = null,
    string? FileName = null,
    long? FileSize = null,
    string? ContentType = null,
    string? Sha256 = null)
{
    /// <summary>Returns a summary suitable for persisting to <c>Calibration.CertDoc</c>.</summary>
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(DocumentName))
            {
                return DocumentName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(CertificateNumber))
            {
                return CertificateNumber.Trim();
            }

            return FileName ?? string.Empty;
        }
    }

    /// <summary>Returns a formatted file size string (e.g. "1.5 MB").</summary>
    public string FileSizeDisplay
    {
        get
        {
            if (FileSize is null || FileSize <= 0)
            {
                return string.Empty;
            }

            return FormatBytes(FileSize.Value);
        }
    }

    internal static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
        double size = bytes;
        int order = 0;

        while (size >= 1024 && order < suffixes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{0:0.##} {1}", size, suffixes[order]);
    }
}

/// <summary>Request payload supplied to the calibration certificate dialog.</summary>
/// <param name="Existing">Existing certificate metadata, if any.</param>
/// <param name="CalibrationDisplay">Optional caption describing the calibration being edited.</param>
/// <param name="AllowFileSelection">When <c>true</c>, the dialog exposes file selection controls.</param>
public sealed record CalibrationCertificateDialogRequest(
    CalibrationCertificateSnapshot? Existing,
    string? CalibrationDisplay,
    bool AllowFileSelection = true);

/// <summary>Result payload returned by the calibration certificate dialog.</summary>
/// <param name="Certificate">The captured certificate metadata.</param>
/// <param name="File">Optional file chosen by the operator for upload.</param>
/// <param name="FileCleared">True when the operator removed the previous certificate file.</param>
public sealed record CalibrationCertificateDialogResult(
    CalibrationCertificateSnapshot Certificate,
    PickedFile? File,
    bool FileCleared,
    int? RemovedAttachmentId);

/// <summary>
/// View-model backing the calibration certificate dialog. Captures certificate metadata, allows
/// the operator to select a file, and exposes the resulting snapshot for the module view-model to
/// persist or attach.
/// </summary>
public sealed partial class CalibrationCertificateDialogViewModel : ObservableObject
{
    private readonly IFilePicker _filePicker;
    private readonly CalibrationCertificateDialogRequest _request;
    private PickedFile? _selectedFile;
    private int? _existingAttachmentId;
    private readonly int? _originalAttachmentId;
    private string? _existingFileName;
    private long? _existingFileSize;
    private string? _existingContentType;
    private string? _existingSha;

    /// <summary>Initializes a new instance of the <see cref="CalibrationCertificateDialogViewModel"/> class.</summary>
    public CalibrationCertificateDialogViewModel(
        CalibrationCertificateDialogRequest request,
        IFilePicker filePicker)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));

        DocumentName = request.Existing?.DocumentName ?? string.Empty;
        CertificateNumber = request.Existing?.CertificateNumber;
        Issuer = request.Existing?.Issuer;
        IssuedOn = request.Existing?.IssuedOn;
        ExpiresOn = request.Existing?.ExpiresOn;
        Notes = request.Existing?.Notes;

        _existingAttachmentId = request.Existing?.AttachmentId;
        _originalAttachmentId = request.Existing?.AttachmentId;
        _existingFileName = request.Existing?.FileName;
        _existingFileSize = request.Existing?.FileSize;
        _existingContentType = request.Existing?.ContentType;
        _existingSha = request.Existing?.Sha256;

        FileName = request.Existing?.FileName;
        FileSizeDisplay = request.Existing?.FileSizeDisplay ?? string.Empty;
        HasExistingFile = (_existingAttachmentId.HasValue && _existingAttachmentId.Value > 0)
            || !string.IsNullOrWhiteSpace(_existingFileName);
        CalibrationDisplay = request.CalibrationDisplay;

        BrowseCommand = new AsyncRelayCommand(BrowseAsync, CanBrowse);
        ClearFileCommand = new RelayCommand(ClearFile, CanClearFile);
        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        CancelCommand = new RelayCommand(Cancel);
    }

    /// <summary>Optional caption describing the calibration context.</summary>
    public string? CalibrationDisplay { get; }

    /// <summary>Result populated when the operator confirms the dialog.</summary>
    public CalibrationCertificateDialogResult? Result { get; private set; }

    /// <summary>Raised when the dialog should close.</summary>
    public event EventHandler<bool>? RequestClose;

    /// <summary>Command that launches the file picker.</summary>
    public IAsyncRelayCommand BrowseCommand { get; }

    /// <summary>Command that clears the selected file.</summary>
    public IRelayCommand ClearFileCommand { get; }

    /// <summary>Command that confirms the dialog.</summary>
    public IRelayCommand ConfirmCommand { get; }

    /// <summary>Command that cancels the dialog.</summary>
    public IRelayCommand CancelCommand { get; }

    [ObservableProperty]
    private string _documentName = string.Empty;

    [ObservableProperty]
    private string? _certificateNumber;

    [ObservableProperty]
    private string? _issuer;

    [ObservableProperty]
    private DateTime? _issuedOn;

    [ObservableProperty]
    private DateTime? _expiresOn;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private string? _fileName;

    [ObservableProperty]
    private string _fileSizeDisplay = string.Empty;

    [ObservableProperty]
    private bool _hasExistingFile;

    [ObservableProperty]
    private bool _hasNewFile;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _statusMessage;

    partial void OnIsBusyChanged(bool value)
        => UpdateCommandStates();

    partial void OnDocumentNameChanged(string value)
        => ConfirmCommand.NotifyCanExecuteChanged();

    private bool CanBrowse() => !IsBusy && _request.AllowFileSelection;

    private bool CanClearFile()
        => !IsBusy && (HasExistingFile || HasNewFile);

    private bool CanConfirm()
        => !IsBusy;

    private async Task BrowseAsync()
    {
        if (!CanBrowse())
        {
            return;
        }

        try
        {
            IsBusy = true;
            StatusMessage = null;

            var request = new FilePickerRequest(
                AllowMultiple: false,
                FileTypes: null,
                Title: string.IsNullOrWhiteSpace(CalibrationDisplay)
                    ? "Select certificate file"
                    : $"Select certificate file for {CalibrationDisplay}");

            var files = await _filePicker
                .PickFilesAsync(request)
                .ConfigureAwait(false);

            var file = files?.FirstOrDefault();
            if (file is null)
            {
                return;
            }

            _selectedFile = file;
            FileName = file.FileName;
            FileSizeDisplay = file.FileSize.HasValue
                ? CalibrationCertificateSnapshot.FormatBytes(file.FileSize.Value)
                : string.Empty;
            HasNewFile = true;
            HasExistingFile = false;
            StatusMessage = $"Selected '{file.FileName}'.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"File selection failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            UpdateCommandStates();
        }
    }

    private void ClearFile()
    {
        if (!CanClearFile())
        {
            return;
        }

        _selectedFile = null;
        HasNewFile = false;
        HasExistingFile = false;
        _existingAttachmentId = null;
        _existingFileName = null;
        _existingFileSize = null;
        _existingContentType = null;
        _existingSha = null;
        FileName = null;
        FileSizeDisplay = string.Empty;
        StatusMessage = "Certificate file cleared.";
        UpdateCommandStates();
    }

    private void Confirm()
    {
        if (!CanConfirm())
        {
            return;
        }

        var docName = string.IsNullOrWhiteSpace(DocumentName)
            ? string.Empty
            : DocumentName.Trim();

        var certificate = new CalibrationCertificateSnapshot(
            docName,
            TrimOrNull(CertificateNumber),
            TrimOrNull(Issuer),
            IssuedOn,
            ExpiresOn,
            TrimOrNull(Notes),
            HasNewFile ? null : _existingAttachmentId,
            HasNewFile ? _selectedFile?.FileName : _existingFileName,
            HasNewFile ? _selectedFile?.FileSize : _existingFileSize,
            HasNewFile ? _selectedFile?.ContentType : _existingContentType,
            HasNewFile ? null : _existingSha);

        bool cleared = !HasNewFile
            && !HasExistingFile
            && _originalAttachmentId.HasValue
            && _originalAttachmentId.Value > 0
            && _existingAttachmentId is null;

        int? removedId = cleared ? _originalAttachmentId : null;

        Result = new CalibrationCertificateDialogResult(certificate, _selectedFile, cleared, removedId);
        RequestClose?.Invoke(this, true);
    }

    private void Cancel()
    {
        Result = null;
        RequestClose?.Invoke(this, false);
    }

    private static string? TrimOrNull(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void UpdateCommandStates()
    {
        BrowseCommand.NotifyCanExecuteChanged();
        ClearFileCommand.NotifyCanExecuteChanged();
        ConfirmCommand.NotifyCanExecuteChanged();
    }
}
