using System.Reflection;

namespace OpenDriverUpdater.App;

public static class AppInfo
{
    /// <summary>Görüntülenebilir sürüm — "0.2.0" veya "0.2.0-beta.1".</summary>
    public static string Version { get; } = GetVersion();

    public static string DisplayName => "OpenDriverUpdater";

    public static string VersionLabel => $"v{Version}";

    private static string GetVersion()
    {
        var informational = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(informational))
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

        // .NET SourceLink sürüme "+<commit hash>" ekler — göstermeden önce ayır.
        var plus = informational.IndexOf('+');
        return plus > 0 ? informational[..plus] : informational;
    }
}