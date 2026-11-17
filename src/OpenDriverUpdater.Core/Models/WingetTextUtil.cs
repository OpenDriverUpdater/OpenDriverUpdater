namespace OpenDriverUpdater.Core.Models;

/// <summary>Helpers for text produced by winget's table output.</summary>
public static class WingetTextUtil
{
    public const char Ellipsis = '…';

    public static bool IsTruncated(string? value) =>
        !string.IsNullOrEmpty(value) &&
        (value.EndsWith(Ellipsis) || value.EndsWith("...", StringComparison.Ordinal));

    /// <summary>Removes winget's trailing truncation marker, leaving the known prefix.</summary>
    public static string StripEllipsis(string value)
    {
        if (value.EndsWith(Ellipsis)) return value[..^1];
        if (value.EndsWith("...", StringComparison.Ordinal)) return value[..^3];
        return value;
    }
}
