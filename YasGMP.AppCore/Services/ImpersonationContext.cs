using System;

namespace YasGMP.Services;

/// <summary>
/// Represents an active impersonation session so callers can persist and later terminate the session.
/// </summary>
/// <param name="ActorUserId">Identifier of the user performing the impersonation.</param>
/// <param name="TargetUserId">Identifier of the impersonated account.</param>
/// <param name="SessionLogId">Primary key of the <c>session_log</c> row tracking the impersonation.</param>
/// <param name="StartedAtUtc">UTC timestamp when the impersonation began.</param>
/// <param name="Reason">Business justification captured when the session started.</param>
/// <param name="Notes">Additional notes captured at start time.</param>
/// <param name="Ip">Originating IP address of the impersonating user.</param>
/// <param name="DeviceInfo">Device fingerprint of the impersonating user.</param>
/// <param name="SessionId">Logical session identifier used for correlated audit events.</param>
/// <param name="SignatureId">Captured electronic signature identifier, when available.</param>
/// <param name="SignatureHash">Hash of the signature payload, when available.</param>
/// <param name="SignatureMethod">Authentication method used when capturing the signature.</param>
/// <param name="SignatureStatus">Recorded status describing the signature validity.</param>
/// <param name="SignatureNote">Free-form note associated with the signature.</param>
public sealed record class ImpersonationContext(
    int ActorUserId,
    int TargetUserId,
    int SessionLogId,
    DateTime StartedAtUtc,
    string Reason,
    string? Notes,
    string? Ip,
    string? DeviceInfo,
    string? SessionId,
    int? SignatureId,
    string? SignatureHash,
    string? SignatureMethod,
    string? SignatureStatus,
    string? SignatureNote);
