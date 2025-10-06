using YasGMP.Models.DTO;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Represents the payload returned from a CRUD adapter after the entity has been persisted.
/// </summary>
public sealed record CrudSaveResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CrudSaveResult"/> record.
    /// </summary>
    /// <param name="id">Unique identifier generated for the persisted entity.</param>
    /// <param name="signatureMetadata">Electronic signature metadata captured alongside the save.</param>
    public CrudSaveResult(int id, SignatureMetadataDto? signatureMetadata)
    {
        Id = id;
        SignatureMetadata = signatureMetadata;
    }

    /// <summary>
    /// Gets the unique identifier for the entity that was saved. Callers should apply this value to
    /// refresh editor state, update navigation contexts, or trigger follow-up lookups that require the
    /// persisted entity's key.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the electronic signature metadata captured during the save operation. Callers should
    /// persist or surface this payload alongside the entity (for example, by updating audit records or
    /// exposing signature details in the UI) to ensure downstream services receive the same signature
    /// context that was accepted at save time.
    /// </summary>
    public SignatureMetadataDto? SignatureMetadata { get; }
}

