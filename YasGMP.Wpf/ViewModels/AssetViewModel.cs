using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using YasGMP.Models;
using YasGMP.Wpf.ViewModels.Modules;

namespace YasGMP.Wpf.ViewModels;

/// <summary>
/// Shared asset editor payload surfaced to the WPF shell.
/// </summary>
public sealed partial class AssetViewModel : SignatureAwareEditor
{
    public event EventHandler? EditorChanged;

    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _model = string.Empty;

    [ObservableProperty]
    private string _manufacturer = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    private string _status = "active";

    [ObservableProperty]
    private string _ursDoc = string.Empty;

    [ObservableProperty]
    private DateTime? _installDate = DateTime.UtcNow.Date;

    [ObservableProperty]
    private DateTime? _procurementDate;

    [ObservableProperty]
    private DateTime? _warrantyUntil;

    [ObservableProperty]
    private bool _isCritical;

    [ObservableProperty]
    private string _serialNumber = string.Empty;

    [ObservableProperty]
    private string _lifecyclePhase = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _qrCode = string.Empty;

    [ObservableProperty]
    private string _qrPayload = string.Empty;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is null)
        {
            return;
        }

        if (ShouldRaiseEditorChanged(e.PropertyName))
        {
            EditorChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private static bool ShouldRaiseEditorChanged(string propertyName)
        => propertyName is nameof(Id)
            or nameof(Code)
            or nameof(Name)
            or nameof(Description)
            or nameof(Model)
            or nameof(Manufacturer)
            or nameof(Location)
            or nameof(Status)
            or nameof(UrsDoc)
            or nameof(InstallDate)
            or nameof(ProcurementDate)
            or nameof(WarrantyUntil)
            or nameof(IsCritical)
            or nameof(SerialNumber)
            or nameof(LifecyclePhase)
            or nameof(Notes)
            or nameof(QrCode)
            or nameof(QrPayload)
            or nameof(SignatureHash)
            or nameof(SignatureReason)
            or nameof(SignatureNote)
            or nameof(SignatureTimestampUtc)
            or nameof(SignerUserId)
            or nameof(SignerUserName)
            or nameof(LastModifiedUtc)
            or nameof(LastModifiedById)
            or nameof(LastModifiedByName)
            or nameof(SourceIp)
            or nameof(SessionId)
            or nameof(DeviceInfo);

    /// <summary>
    /// Clears all fields to their neutral defaults.
    /// </summary>
    public void Reset()
    {
        Id = 0;
        Code = string.Empty;
        Name = string.Empty;
        Description = string.Empty;
        Model = string.Empty;
        Manufacturer = string.Empty;
        Location = string.Empty;
        Status = "active";
        UrsDoc = string.Empty;
        InstallDate = DateTime.UtcNow.Date;
        ProcurementDate = null;
        WarrantyUntil = null;
        IsCritical = false;
        SerialNumber = string.Empty;
        LifecyclePhase = string.Empty;
        Notes = string.Empty;
        QrCode = string.Empty;
        QrPayload = string.Empty;
        SignatureHash = string.Empty;
        SignatureReason = string.Empty;
        SignatureNote = string.Empty;
        SignatureTimestampUtc = null;
        SignerUserId = null;
        SignerUserName = string.Empty;
        LastModifiedUtc = null;
        LastModifiedById = null;
        LastModifiedByName = string.Empty;
        SourceIp = string.Empty;
        SessionId = string.Empty;
        DeviceInfo = string.Empty;
    }

    /// <summary>
    /// Prepares the view-model for Add mode.
    /// </summary>
    public void InitializeForNew(string normalizedStatus)
    {
        Reset();
        Status = normalizedStatus ?? string.Empty;
        LastModifiedUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Populates the view-model from an existing machine entity.
    /// </summary>
    public void LoadFromMachine(Machine machine, Func<string?, string> normalizer)
    {
        if (machine is null)
        {
            throw new ArgumentNullException(nameof(machine));
        }

        if (normalizer is null)
        {
            throw new ArgumentNullException(nameof(normalizer));
        }

        Id = machine.Id;
        Code = machine.Code ?? string.Empty;
        Name = machine.Name ?? string.Empty;
        Description = machine.Description ?? string.Empty;
        Model = machine.Model ?? string.Empty;
        Manufacturer = machine.Manufacturer ?? string.Empty;
        Location = machine.Location ?? string.Empty;
        Status = normalizer(machine.Status);
        UrsDoc = machine.UrsDoc ?? string.Empty;
        InstallDate = machine.InstallDate;
        ProcurementDate = machine.ProcurementDate;
        WarrantyUntil = machine.WarrantyUntil;
        IsCritical = machine.IsCritical;
        SerialNumber = machine.SerialNumber ?? string.Empty;
        LifecyclePhase = machine.LifecyclePhase ?? string.Empty;
        Notes = machine.Note ?? string.Empty;
        QrCode = machine.QrCode ?? string.Empty;
        QrPayload = machine.QrPayload ?? string.Empty;
        SignatureHash = machine.DigitalSignature ?? string.Empty;
        SignatureTimestampUtc = machine.LastModified;
        SignerUserId = machine.LastModifiedById;
        SignerUserName = machine.LastModifiedBy?.FullName ?? string.Empty;
        LastModifiedUtc = machine.LastModified;
        LastModifiedById = machine.LastModifiedById;
        LastModifiedByName = machine.LastModifiedBy?.FullName ?? string.Empty;
        SignatureReason = string.Empty;
        SignatureNote = string.Empty;
        SourceIp = string.Empty;
        SessionId = string.Empty;
        DeviceInfo = string.Empty;
    }

    /// <summary>
    /// Converts the view-model into a machine entity for persistence.
    /// </summary>
    public Machine ToMachine(Machine? existing)
    {
        var machine = existing is null ? new Machine() : CloneMachine(existing);
        machine.Id = Id;
        machine.Code = Code;
        machine.Name = Name;
        machine.Description = Description;
        machine.Model = Model;
        machine.Manufacturer = Manufacturer;
        machine.Location = Location;
        machine.Status = Status;
        machine.UrsDoc = UrsDoc;
        machine.InstallDate = InstallDate;
        machine.ProcurementDate = ProcurementDate;
        machine.WarrantyUntil = WarrantyUntil;
        machine.IsCritical = IsCritical;
        machine.SerialNumber = string.IsNullOrWhiteSpace(SerialNumber) ? machine.SerialNumber : SerialNumber;
        machine.LifecyclePhase = LifecyclePhase;
        machine.Note = Notes;
        machine.QrCode = string.IsNullOrWhiteSpace(QrCode) ? null : QrCode.Trim();
        machine.QrPayload = string.IsNullOrWhiteSpace(QrPayload) ? null : QrPayload.Trim();
        machine.DigitalSignature = SignatureHash;
        machine.LastModified = LastModifiedUtc ?? DateTime.UtcNow;
        machine.LastModifiedById = LastModifiedById ?? machine.LastModifiedById;
        return machine;
    }

    private static Machine CloneMachine(Machine source)
    {
        return new Machine
        {
            Id = source.Id,
            Code = source.Code,
            Name = source.Name,
            Description = source.Description,
            Model = source.Model,
            Manufacturer = source.Manufacturer,
            Location = source.Location,
            Status = source.Status,
            UrsDoc = source.UrsDoc,
            InstallDate = source.InstallDate,
            ProcurementDate = source.ProcurementDate,
            WarrantyUntil = source.WarrantyUntil,
            IsCritical = source.IsCritical,
            SerialNumber = source.SerialNumber,
            LifecyclePhase = source.LifecyclePhase,
            Note = source.Note,
            QrCode = source.QrCode,
            QrPayload = source.QrPayload
        };
    }
}
