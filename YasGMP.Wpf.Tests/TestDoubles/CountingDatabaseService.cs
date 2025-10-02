using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using YasGMP.Models;
using YasGMP.Services;

namespace YasGMP.Wpf.Tests.TestDoubles;

/// <summary>
/// In-memory DatabaseService double that tracks digital signature persistence attempts.
/// </summary>
public class CountingDatabaseService : DatabaseService
{
    private int _nextSignatureId = 1;

    /// <summary>Gets or sets the next identifier assigned to a simulated signature insert.</summary>
    public int NextSignatureId
    {
        get => _nextSignatureId;
        set => _nextSignatureId = value > 0 ? value : 1;
    }

    public CountingDatabaseService()
    {
    }

    /// <summary>Gets the number of times a signature insert was requested.</summary>
    public int InsertDigitalSignatureCallCount { get; private set; }

    /// <summary>Gets the captured signature payloads that were "persisted" during the test.</summary>
    public List<DigitalSignature> InsertedSignatures { get; } = new();

    /// <summary>
    /// Simulates inserting a digital signature record and assigns a sequential identifier when needed.
    /// </summary>
    public virtual Task<int> InsertDigitalSignatureAsync(
        DigitalSignature signature,
        CancellationToken cancellationToken = default)
    {
        if (signature is null)
        {
            throw new ArgumentNullException(nameof(signature));
        }

        InsertDigitalSignatureCallCount++;

        if (signature.Id <= 0)
        {
            signature.Id = _nextSignatureId++;
        }

        InsertedSignatures.Add(CloneSignature(signature));
        return Task.FromResult(signature.Id);
    }

    private static DigitalSignature CloneSignature(DigitalSignature signature)
        => new()
        {
            Id = signature.Id,
            TableName = signature.TableName,
            RecordId = signature.RecordId,
            UserId = signature.UserId,
            SignatureHash = signature.SignatureHash,
            Method = signature.Method,
            Status = signature.Status,
            SignedAt = signature.SignedAt,
            DeviceInfo = signature.DeviceInfo,
            IpAddress = signature.IpAddress,
            Note = signature.Note,
            SessionId = signature.SessionId,
            UserName = signature.UserName,
            PublicKey = signature.PublicKey
        };
}
