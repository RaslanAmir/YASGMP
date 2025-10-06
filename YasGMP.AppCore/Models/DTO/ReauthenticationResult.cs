namespace YasGMP.Models.DTO
{
    /// <summary>
    /// Result returned from the re-authentication dialog before applying a work-order signature.
    /// </summary>
    /// <param name="Username">Username entered by the operator.</param>
    /// <param name="Password">Password entered by the operator.</param>
    /// <param name="MfaCode">Optional MFA/TOTP code supplied for second factor verification.</param>
    /// <param name="ReasonCode">Normalized GMP reason code for the signature.</param>
    /// <param name="ReasonDetail">Free-form justification/note tied to the reason code.</param>
    /// <param name="ReasonDisplay">Human readable caption of the selected reason.</param>
    public sealed record ReauthenticationResult(
        string Username,
        string Password,
        string? MfaCode,
        string ReasonCode,
        string? ReasonDetail,
        string ReasonDisplay);
}


