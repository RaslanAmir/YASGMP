using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace YasGMP.Wpf.ViewModels.Modules;

/// <summary>
/// Base editor payload that surfaces e-signature metadata captured during persistence.
/// </summary>
public abstract partial class SignatureAwareEditor : ObservableObject
{
    [ObservableProperty]
    private string _signatureHash = string.Empty;

    [ObservableProperty]
    private int? _signerUserId;

    [ObservableProperty]
    private string _signerUserName = string.Empty;

    [ObservableProperty]
    private string _signatureReason = string.Empty;

    [ObservableProperty]
    private string _signatureNote = string.Empty;

    [ObservableProperty]
    private DateTime? _signatureTimestampUtc;

    [ObservableProperty]
    private DateTime? _lastModifiedUtc;

    [ObservableProperty]
    private int? _lastModifiedById;

    [ObservableProperty]
    private string _lastModifiedByName = string.Empty;

    [ObservableProperty]
    private string _sourceIp = string.Empty;

    [ObservableProperty]
    private string _sessionId = string.Empty;

    [ObservableProperty]
    private string _deviceInfo = string.Empty;
}
