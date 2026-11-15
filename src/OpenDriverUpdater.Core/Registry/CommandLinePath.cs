namespace OpenDriverUpdater.Core.Registry;

/// <summary>
/// Pulls the program path out of a Windows command line such as
/// <c>"C:\Program Files\App\app.exe" --tray</c>.
///
/// Deliberately conservative: it only returns absolute paths. Bare names
/// (<c>msiexec.exe /X{...}</c>, <c>ctfmon.exe</c>) are resolved by Windows
/// through system folders and PATH, so they can't be judged as missing and
/// return null. Uses its own Windows-path handling so it behaves the same on
/// every OS (System.IO.Path follows the host OS's rules).
/// </summary>
public static class CommandLinePath
{
    private static readonly string[] ProgramExtensions = [".exe", ".com", ".bat", ".cmd", ".scr", ".dll", ".cpl", ".lnk", ".vbs", ".ps1"];

    /// <param name="command">Command line with environment variables already expanded.</param>
    public static string? ExtractProgramPath(string? command)
    {
        if (string.IsNullOrWhiteSpace(command)) return null;
        var s = command.Trim();

        var (program, rest) = SplitFirst(s);
        if (program is null) return null;

        // rundll32 <dll>,<entry>: the DLL is what has to exist.
        var programName = FileName(program);
        if (programName.Equals("rundll32.exe", StringComparison.OrdinalIgnoreCase) ||
            programName.Equals("rundll32", StringComparison.OrdinalIgnoreCase))
        {
            var args = rest.Trim();
            string dll;
            if (args.StartsWith('"'))
            {
                var end = args.IndexOf('"', 1);
                dll = end > 0 ? args[1..end] : args[1..];
            }
            else
            {
                var comma = args.IndexOf(',');
                dll = comma >= 0 ? args[..comma] : args;
            }
            dll = dll.Trim();
            return IsAbsoluteWindowsPath(dll) ? dll : null;
        }

        return IsAbsoluteWindowsPath(program) ? program : null;
    }

    /// <summary>Splits off the program part: quoted, or unquoted up to a known extension or the first space.</summary>
    private static (string? Program, string Rest) SplitFirst(string s)
    {
        if (s.StartsWith('"'))
        {
            var end = s.IndexOf('"', 1);
            if (end < 0) return (s[1..], string.Empty);   // unbalanced quote
            var quoted = s[1..end];
            return (quoted.Length == 0 ? null : quoted, s[(end + 1)..]);
        }

        // "rundll32 x.dll,Entry" (no .exe): the first token is the program.
        var firstSpace = s.IndexOf(' ');
        var firstToken = firstSpace >= 0 ? s[..firstSpace] : s;
        if (FileName(firstToken).Equals("rundll32", StringComparison.OrdinalIgnoreCase))
            return (firstToken, firstSpace >= 0 ? s[firstSpace..] : string.Empty);

        // Unquoted paths may contain spaces (C:\Program Files\x\app.exe -a):
        // cut after the first program extension that ends a token.
        var best = -1;
        foreach (var ext in ProgramExtensions)
        {
            var from = 0;
            while (true)
            {
                var i = s.IndexOf(ext, from, StringComparison.OrdinalIgnoreCase);
                if (i < 0) break;
                var after = i + ext.Length;
                if (after == s.Length || s[after] is ' ' or ',' or '"' or '\t')
                {
                    if (best < 0 || after < best) best = after;
                    break;
                }
                from = i + 1;
            }
        }

        if (best > 0)
        {
            var candidate = s[..best];
            // "C:\Tools\tool -cfg C:\x\a.cmd": the cut swallowed switches, so
            // the real program is unclear. Don't guess.
            if (candidate.Contains(" -") || candidate.Contains(" /"))
                return (null, string.Empty);
            return (candidate, s[best..]);
        }

        var space = s.IndexOf(' ');
        return space >= 0 ? (s[..space], s[space..]) : (s, string.Empty);
    }

    public static bool IsAbsoluteWindowsPath(string path) =>
        (path.Length >= 3 && char.IsAsciiLetter(path[0]) && path[1] == ':' && (path[2] is '\\' or '/'))
        || path.StartsWith(@"\\", StringComparison.Ordinal);

    public static string FileName(string path)
    {
        var i = path.LastIndexOfAny(['\\', '/']);
        return i >= 0 ? path[(i + 1)..] : path;
    }
}
