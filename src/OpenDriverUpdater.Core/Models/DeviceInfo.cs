using OpenDriverUpdater.Core.Enums;

namespace OpenDriverUpdater.Core.Models;

/// <summary>
/// Sistemde tespit edilen bir donanım cihazının bilgileri.
/// </summary>
public sealed class DeviceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DeviceClass Class { get; set; } = DeviceClass.Unknown;
    public string? Manufacturer { get; set; }
    public IReadOnlyList<string> HardwareIds { get; set; } = [];
    public IReadOnlyList<string> CompatibleIds { get; set; } = [];
    public string? InstalledDriverVersion { get; set; }
    public DateTime? InstalledDriverDate { get; set; }
    public string? InstalledProvider { get; set; }

    public override string ToString() => $"{Name} [{Class}] v{InstalledDriverVersion ?? "N/A"}";
}
