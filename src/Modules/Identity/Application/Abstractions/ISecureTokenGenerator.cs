namespace BUnited.Modules.Identity.Application.Abstractions;

/// <summary>
/// Generates opaque, cryptographically random tokens for refresh/email-verification/
/// password-reset flows. Only <see cref="Hash"/> of the raw value is ever persisted
/// (docs/DEVELOPMENT_INSTRUCTIONS.md §6) — the raw value is returned to the caller once.
/// </summary>
public interface ISecureTokenGenerator
{
    (string RawToken, string TokenHash) Generate();

    string Hash(string rawToken);
}
