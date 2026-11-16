namespace OpenDriverUpdater.Core.Models;

/// <summary>
/// An application installed on the PC, as reported by winget (Windows Package Manager).
/// </summary>
public sealed class InstalledApp
{
    public string Name { get; set; } = string.Empty;

    /// <summary>winget package ID, or an ARP/MSIX identifier for apps winget can't match to a source.</summary>
    public string Id { get; set; } = string.Empty;

    public string? InstalledVersion { get; set; }

    /// <summary>Newer version available from <see cref="Source"/>; empty when none.</summary>
    public string? AvailableVersion { get; set; }

    /// <summary>winget source (e.g. "winget", "msstore"). Empty when winget can't track this app.</summary>
    public string? Source { get; set; }

    /// <summary>winget knows where this app comes from, so it can tell whether it's up to date.</summary>
    public bool IsTracked => !string.IsNullOrWhiteSpace(Source);

    public bool HasUpdate => !string.IsNullOrWhiteSpace(AvailableVersion);

    /// <summary>winget shortened the ID to fit its table (ends with an ellipsis).</summary>
    public bool IsIdTruncated => WingetTextUtil.IsTruncated(Id);

    public override string ToString() =>
        $"{Name} [{Id}] {InstalledVersion}{(HasUpdate ? $" -> {AvailableVersion}" : "")}";
}
