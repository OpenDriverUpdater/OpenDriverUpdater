using OpenDriverUpdater.Core.Enums;

namespace OpenDriverUpdater.Core.Models;

/// <summary>
/// Sürücü güven puanı.
/// </summary>
public sealed class TrustScore
{
    public TrustLevel Level { get; init; } = TrustLevel.Unknown;
    public int Points { get; init; }
    public IReadOnlyList<string> Details { get; init; } = [];

    public override string ToString() => $"{Level} ({Points} pts)";
}
