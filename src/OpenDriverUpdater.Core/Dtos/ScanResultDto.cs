namespace OpenDriverUpdater.Core.Dtos;

using OpenDriverUpdater.Core.Models;

/// <summary>
/// Tarama sonucu DTO'su.
/// </summary>
public sealed class ScanResultDto
{
    public IReadOnlyList<DeviceInfo> Devices { get; init; } = [];
    public int TotalCount => Devices.Count;
    public DateTime ScannedAt { get; init; } = DateTime.UtcNow;
    public TimeSpan Duration { get; init; }
}
