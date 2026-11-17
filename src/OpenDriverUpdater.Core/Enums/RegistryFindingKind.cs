namespace OpenDriverUpdater.Core.Enums;

public enum RegistryFindingKind
{
    /// <summary>A Run/RunOnce value that starts a program at sign-in.</summary>
    StartupEntry,

    /// <summary>An Add/Remove Programs (Uninstall) entry.</summary>
    InstalledProgram,
}
