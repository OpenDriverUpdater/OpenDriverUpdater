using OpenDriverUpdater.Core.Enums;

namespace OpenDriverUpdater.Core.Models;

/// <summary>
/// Sistemde tespit edilen bir donanım cihazının bilgileri.
/// </summary>
public sealed class DeviceInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public DeviceClass Class { get; init; } = DeviceClass.Unknown;
    public string? Manufacturer { get; init; }
    public IReadOnlyList<string> HardwareIds { get; init; } = [];
    public IReadOnlyList<string> CompatibleIds { get; init; } = [];
    public string? InstalledDriverVersion { get; init; }
    public DateTime? InstalledDriverDate { get; init; }
    public string? InstalledProvider { get; init; }

    public override string ToString() => $"{Name} [{Class}] v{InstalledDriverVersion ?? "N/A"}";
}
