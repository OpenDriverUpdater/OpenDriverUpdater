using OpenDriverUpdater.Core.Enums;

namespace OpenDriverUpdater.Core.Models;

/// <summary>A registry entry whose command points to a file that doesn't exist.</summary>
public sealed record RegistryFinding(RegistryEntry Entry, string MissingPath)
{
    public RegistryFindingKind Kind => Entry.Kind;
    public string Name => Entry.Name;
}

/// <summary>Result of a read-only registry health scan.</summary>
public sealed record RegistryScanResult(
    int StartupEntriesChecked,
    int ProgramsChecked,
    IReadOnlyList<RegistryFinding> Findings);
