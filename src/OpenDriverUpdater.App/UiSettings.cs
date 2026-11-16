using System.Text.Json;

namespace OpenDriverUpdater.App;

/// <summary>
/// Small per-user UI preferences (currently just the navigation pane width),
/// stored in %LocalAppData%\OpenDriverUpdater\ui.json. Missing or corrupt
/// files fall back to defaults; writes are atomic (temp file + move).
/// </summary>
public sealed class UiSettings
{
    public const double DefaultPaneLength = 240;
    public const double MinPaneLength = 180;
    public const double MaxPaneLength = 480;

    public int Version { get; set; } = 1;
    public double NavPaneLength { get; set; } = DefaultPaneLength;

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "OpenDriverUpdater", "ui.json");

    public static double ClampPaneLength(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, MinPaneLength, MaxPaneLength) : DefaultPaneLength;

    public static UiSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var loaded = JsonSerializer.Deserialize<UiSettings>(File.ReadAllText(FilePath));
                if (loaded is not null)
                {
                    loaded.NavPaneLength = ClampPaneLength(loaded.NavPaneLength);
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            App.Logger?.Warn("Could not read UI settings, using defaults: {Error}", ex.Message);
        }

        return new UiSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var temp = FilePath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, FilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            App.Logger?.Warn("Could not save UI settings: {Error}", ex.Message);
        }
    }
}
