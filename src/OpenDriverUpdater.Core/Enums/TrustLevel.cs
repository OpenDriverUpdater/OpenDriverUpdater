namespace OpenDriverUpdater.Core.Enums;

/// <summary>
/// Sürücü güven seviyeleri.
/// </summary>
public enum TrustLevel
{
    Blacklisted = 0,
    Untrusted,
    Unknown,
    Trusted,
    Verified
}
