using System.Diagnostics;
using System.Linq;
using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Infrastructure.Hardware;

/// <summary>
/// PnP cihaz enumerasyonu — pnputil /enum-devices çıktısını ayrıştırır.
/// </summary>
public sealed class PnpHardwareScanner : IHardwareScanner
{
    private readonly ILoggerService _logger;

    public PnpHardwareScanner(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<DeviceInfo>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var devices = new List<DeviceInfo>();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "pnputil",
                Arguments = "/enum-devices /connected",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                _logger.Error("Failed to start pnputil process");
                return devices;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            devices = ParsePnpOutput(output);
            // Phase 2 processing
            devices = FilterNoise(devices);
            ImproveClassification(devices);
            devices = GroupDuplicates(devices);

            _logger.Info("PnP scan completed: {Count} devices found", devices.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Error("PnP scan failed", ex);
        }

        return devices;
    }

    private List<DeviceInfo> ParsePnpOutput(string output)
    {
        var devices = new List<DeviceInfo>();
        var blocks = output.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var block in blocks)
        {
            var lines = block.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var line in lines)
            {
                var colonIdx = line.IndexOf(':');
                if (colonIdx <= 0) continue;

                var key = line[..colonIdx].Trim();
                var value = line[(colonIdx + 1)..].Trim();
                dict[key] = value;
            }

            if (!dict.TryGetValue("Instance ID", out var instanceId)) continue;

            var name = dict.GetValueOrDefault("Device Description", "Unknown Device");
            var deviceClass = dict.GetValueOrDefault("Class", "");

            var dev = new DeviceInfo
            {
                Id = instanceId,
                Name = name,
                Class = ParseDeviceClassFromPnp(deviceClass, name, instanceId),
                Manufacturer = dict.GetValueOrDefault("Manufacturer"),
                HardwareIds = string.IsNullOrWhiteSpace(instanceId) ? [] : [instanceId],
                InstalledDriverVersion = dict.GetValueOrDefault("Driver Version"),
                InstalledProvider = dict.GetValueOrDefault("Driver Provider Name")
            };

            devices.Add(dev);
        }

        return devices;
    }

    private static DeviceClass ParseDeviceClassFromPnp(string pnpClass, string name, string instanceId)
    {
        var cls = pnpClass?.Trim().ToUpperInvariant();
        return cls switch
        {
            "DISPLAY" => DeviceClass.Display,
            "NET" => DeviceClass.Network,
            "MEDIA" or "AUDIOENDPOINT" => DeviceClass.Audio,
            "USB" => DeviceClass.USB,
            "BLUETOOTH" => DeviceClass.Bluetooth,
            "DISKDRIVE" => DeviceClass.Storage,
            "HIDCLASS" => DeviceClass.Input,
            "IMAGE" => DeviceClass.Camera,
            "SYSTEM" => DeviceClass.System,
            _ => DeviceClass.Unknown
        };
    }

    private static List<DeviceInfo> FilterNoise(IReadOnlyList<DeviceInfo> devices)
    {
        var noiseKeywords = new[] { "acpi", "root", "swd", "bthle", "standard", "basic", "generic", "enumerator", "composite" };
        var filtered = new List<DeviceInfo>();

        foreach (var d in devices)
        {
            var name = d.Name ?? "";
            var manufacturer = d.Manufacturer ?? "";

            if (d.HardwareIds.Any(id => id.StartsWith("ACPI\\", StringComparison.OrdinalIgnoreCase) ||
                                         id.StartsWith("ROOT\\", StringComparison.OrdinalIgnoreCase) ||
                                         id.StartsWith("SWD\\", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var isMicrosoftGeneric = manufacturer.Equals("Microsoft", StringComparison.OrdinalIgnoreCase) &&
                                     noiseKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (isMicrosoftGeneric)
                continue;

            filtered.Add(d);
        }

        return filtered;
    }

    private static void ImproveClassification(List<DeviceInfo> devices)
    {
        foreach (var d in devices)
        {
            if (d.Class != DeviceClass.Unknown)
                continue;

            var name = d.Name ?? "";
            var hw = string.Join("|", d.HardwareIds);

            if (name.Contains("graphics", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("nvidia", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("amd", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("radeon", StringComparison.OrdinalIgnoreCase))
            {
                d.Class = DeviceClass.Display;
            }
            else if (name.Contains("wireless", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("wi-fi", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("wifi", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("ethernet", StringComparison.OrdinalIgnoreCase))
            {
                d.Class = DeviceClass.Network;
            }
            else if (name.Contains("audio", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("sound", StringComparison.OrdinalIgnoreCase))
            {
                d.Class = DeviceClass.Audio;
            }
            else if (name.Contains("disk", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("storage", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("nvme", StringComparison.OrdinalIgnoreCase))
            {
                d.Class = DeviceClass.Storage;
            }
        }
    }

    private static List<DeviceInfo> GroupDuplicates(IReadOnlyList<DeviceInfo> devices)
    {
        var map = new Dictionary<string, DeviceInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in devices)
        {
            var manufacturer = d.Manufacturer ?? "";
            var name = NormalizeDeviceName(d.Name);
            var key = $"{manufacturer}||{name}";
            if (!map.ContainsKey(key))
                map[key] = d;
        }
        return map.Values.ToList();
    }

    private static string NormalizeDeviceName(string name)
    {
        var trimmed = name.Trim();
        trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\s*\(\d+\)$", "");
        trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\s*#\d+$", "");
        return trimmed;
    }
}
