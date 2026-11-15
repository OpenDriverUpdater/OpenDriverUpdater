using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Registry;

/// <summary>
/// Decides whether a registry entry points to a missing file. File-system
/// access is passed in so the rules are testable and the caller controls
/// what counts as "checkable".
/// </summary>
public static class RegistryHealthRules
{
    /// <param name="entry">The entry to check.</param>
    /// <param name="expandEnvironment">Expands %VAR% references.</param>
    /// <param name="fileExists">True if the file exists.</param>
    /// <param name="isOnCheckableDrive">
    /// True only for paths on a local fixed drive that is present. Paths on
    /// removable, network or missing drives are never reported: the file may
    /// simply be unplugged or offline.
    /// </param>
    /// <returns>A finding, or null if the entry is fine or can't be judged.</returns>
    public static RegistryFinding? Check(
        RegistryEntry entry,
        Func<string, string> expandEnvironment,
        Func<string, bool> fileExists,
        Func<string, bool> isOnCheckableDrive)
    {
        var command = entry.Command;
        if (string.IsNullOrWhiteSpace(command)) return null;

        // A 32-bit program sees %ProgramFiles% as "Program Files (x86)".
        if (entry.Is32BitView)
        {
            command = ReplaceIgnoreCase(command, "%ProgramFiles%", "%ProgramFiles(x86)%");
            command = ReplaceIgnoreCase(command, "%CommonProgramFiles%", "%CommonProgramFiles(x86)%");
        }

        var path = CommandLinePath.ExtractProgramPath(expandEnvironment(command));
        if (path is null) return null;
        if (!isOnCheckableDrive(path)) return null;
        if (fileExists(path)) return null;

        // A 32-bit program asking for System32 is redirected to SysWOW64.
        if (entry.Is32BitView)
        {
            var redirected = ReplaceIgnoreCase(path, @"\System32\", @"\SysWOW64\");
            if (!redirected.Equals(path, StringComparison.Ordinal) && fileExists(redirected))
                return null;
        }

        return new RegistryFinding(entry, path);
    }

    private static string ReplaceIgnoreCase(string s, string find, string replacement) =>
        s.Replace(find, replacement, StringComparison.OrdinalIgnoreCase);
}
