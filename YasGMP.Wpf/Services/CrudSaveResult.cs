using YasGMP.AppCore.Models.Signatures;

namespace YasGMP.Wpf.Services;

/// <summary>
/// Represents the payload returned from a CRUD adapter after the entity has been persisted.
/// </summary>
/// <typeparam name="TIdentifier">Type used to uniquely identify the saved entity.</typeparam>
public sealed record CrudSaveResult<TIdentifier>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CrudSaveResult{TIdentifier}"/> record.
    /// </summary>
    /// <param name="identifier">Unique identifier generated for the persisted entity.</param>
    /// <param name="signatureMetadata">Electronic signature metadata captured alongside the save.</param>
    public CrudSaveResult(TIdentifier identifier, SignatureMetadataDto? signatureMetadata)
    {
        Identifier = identifier;
        SignatureMetadata = signatureMetadata;
    }

    /// <summary>
    /// Gets the unique identifier for the entity that was saved. Callers should apply this value to
    /// refresh editor state, update navigation contexts, or trigger follow-up lookups that require the
    /// persisted entity's key.
    /// </summary>
    public TIdentifier Identifier { get; }

    /// <summary>
    /// Gets the electronic signature metadata captured during the save operation. Callers should
    /// persist or surface this payload alongside the entity (for example, by updating audit records or
    /// exposing signature details in the UI) to ensure downstream services receive the same signature
    /// context that was accepted at save time.
    /// </summary>
    public SignatureMetadataDto? SignatureMetadata { get; }
}
