#if WINDOWS
using System.Management;
#endif
using System.Linq;
using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Infrastructure.Hardware;

/// <summary>
/// WMI tabanlı donanım tarayıcısı — Win32_PnPSignedDriver sorgusu kullanır.
/// </summary>
public sealed class WmiHardwareScanner : IHardwareScanner
{
    private readonly ILoggerService _logger;

    public WmiHardwareScanner(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IReadOnlyList<DeviceInfo>> ScanAsync(CancellationToken cancellationToken = default)
    {
        var devices = new List<DeviceInfo>();

#if WINDOWS
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPSignedDriver WHERE DeviceName IS NOT NULL");

            foreach (var obj in searcher.Get())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var deviceId = obj["DeviceID"]?.ToString();
                var deviceName = obj["DeviceName"]?.ToString();

                if (string.IsNullOrWhiteSpace(deviceId) || string.IsNullOrWhiteSpace(deviceName))
                    continue;

                var device = new DeviceInfo
                {
                    Id = deviceId,
                    Name = deviceName,
                    Class = ParseDeviceClass(obj["DeviceClass"]?.ToString()),
                    Manufacturer = obj["Manufacturer"]?.ToString(),
                    HardwareIds = ExtractHardwareIds(deviceId),
                    InstalledDriverVersion = obj["DriverVersion"]?.ToString(),
                    InstalledDriverDate = ParseDriverDate(obj["DriverDate"]?.ToString()),
                    InstalledProvider = obj["DriverProviderName"]?.ToString()
                };

                devices.Add(device);
            }

            // Phase 2: Noise reduction, improved classification, smart grouping
            var filtered = FilterNoise(devices);
            var classified = ImproveClassification(filtered);
            var grouped = GroupDuplicates(classified);

            _logger.Info("WMI scan completed: {Raw} devices found, {Filtered} after processing", devices.Count, grouped.Count);

            devices.Clear();
            devices.AddRange(grouped);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Error("WMI scan failed", ex);
        }
#else
        _logger.Warn("WmiHardwareScanner is not supported on this platform.");
#endif

        return Task.FromResult<IReadOnlyList<DeviceInfo>>(devices);
    }

    private static DeviceClass ParseDeviceClass(string? className) => className?.ToUpperInvariant() switch
    {
        "DISPLAY" => DeviceClass.Display,
        "NET" => DeviceClass.Network,
        "MEDIA" or "AUDIOENDPOINT" => DeviceClass.Audio,
        "USB" => DeviceClass.USB,
        "BLUETOOTH" => DeviceClass.Bluetooth,
        "DISKDRIVE" or "HDDCONTROLLER" => DeviceClass.Storage,
        "KEYBOARD" or "MOUSE" or "HIDCLASS" => DeviceClass.Input,
        "PRINTER" => DeviceClass.Printer,
        "CAMERA" or "IMAGE" => DeviceClass.Camera,
        "BIOMETRIC" => DeviceClass.Biometric,
        "FIRMWARE" => DeviceClass.Firmware,
        "SYSTEM" => DeviceClass.System,
        _ => DeviceClass.Unknown,
    };

    private static IReadOnlyList<string> ExtractHardwareIds(string deviceId)
    {
        // DeviceID genellikle birincil Hardware ID içerir.
        return string.IsNullOrWhiteSpace(deviceId) ? [] : [deviceId];
    }

    private static DateTime? ParseDriverDate(string? dateStr)
    {
        if (string.IsNullOrWhiteSpace(dateStr)) return null;

        // WMI tarih formatı: yyyyMMddHHmmss.ffffff+zzz
        var clean = dateStr.Split('.')[0];
        return DateTime.TryParseExact(clean, "yyyyMMddHHmmss",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var result) ? result : null;
    }

    private static List<DeviceInfo> FilterNoise(IReadOnlyList<DeviceInfo> devices)
    {
        var noiseKeywords = new[] { "acpi", "root", "swd", "bthle", "standard", "basic", "generic", "enumerator", "composite" };
        var filtered = new List<DeviceInfo>();

        foreach (var d in devices)
        {
            var name = d.Name ?? "";
            var manufacturer = d.Manufacturer ?? "";

            // Filter ACPI / system spam by HardwareId prefix
            if (d.HardwareIds.Any(id => id.StartsWith("ACPI\\", StringComparison.OrdinalIgnoreCase) ||
                                         id.StartsWith("ROOT\\", StringComparison.OrdinalIgnoreCase) ||
                                         id.StartsWith("SWD\\", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            // Filter Microsoft generic devices
            var isMicrosoftGeneric = manufacturer.Equals("Microsoft", StringComparison.OrdinalIgnoreCase) &&
                                     noiseKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (isMicrosoftGeneric)
                continue;

            // Filter devices with empty name or manufacturer = "Generic"
            if (manufacturer.Equals("Generic", StringComparison.OrdinalIgnoreCase) && noiseKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase)))
                continue;

            filtered.Add(d);
        }

        return filtered;
    }

    private static List<DeviceInfo> ImproveClassification(IReadOnlyList<DeviceInfo> devices)
    {
        foreach (var d in devices)
        {
            if (d.Class != DeviceClass.Unknown)
                continue;

            var name = d.Name ?? "";
            var hw = string.Join("|", d.HardwareIds);

            // Heuristic classification from name / hardware id
            if (name.Contains("graphics", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("nvidia", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("amd", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("radeon", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("intel hd", StringComparison.OrdinalIgnoreCase) ||
                hw.Contains("VEN_10DE", StringComparison.OrdinalIgnoreCase) ||
                hw.Contains("VEN_1002", StringComparison.OrdinalIgnoreCase))
            {
                d.Class = DeviceClass.Display;
            }
            else if (name.Contains("wireless", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("wi-fi", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("wifi", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("ethernet", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("network adapter", StringComparison.OrdinalIgnoreCase) ||
                     hw.Contains("VEN_14E4", StringComparison.OrdinalIgnoreCase) ||
                     hw.Contains("NET\\"))
            {
                d.Class = DeviceClass.Network;
            }
            else if (name.Contains("audio", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("sound", StringComparison.OrdinalIgnoreCase) ||
                     hw.Contains("HDAUDIO\\"))
            {
                d.Class = DeviceClass.Audio;
            }
            else if (name.Contains("disk", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("storage", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("sata", StringComparison.OrdinalIgnoreCase) ||
                     name.Contains("nvme", StringComparison.OrdinalIgnoreCase) ||
                     hw.Contains("SCSI\\") ||
                     hw.Contains("DISKDRIVE"))
            {
                d.Class = DeviceClass.Storage;
            }
            else if (name.Contains("usb", StringComparison.OrdinalIgnoreCase) ||
                     hw.Contains("USB\\VID_"))
            {
                d.Class = DeviceClass.USB;
            }
        }

        return devices.ToList();
    }

    private static List<DeviceInfo> GroupDuplicates(IReadOnlyList<DeviceInfo> devices)
    {
        var map = new Dictionary<string, DeviceInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var d in devices)
        {
            var manufacturer = d.Manufacturer ?? "";
            var name = NormalizeDeviceName(d.Name);

            var key = $"{manufacturer}||{name}";

            if (map.TryGetValue(key, out var existing))
            {
                // Keep the entry with newer driver version or more info
                if (CompareVersions(d.InstalledDriverVersion, existing.InstalledDriverVersion) > 0)
                {
                    map[key] = d;
                }
            }
            else
            {
                map[key] = d;
            }
        }

        return map.Values.ToList();
    }

    private static string NormalizeDeviceName(string name)
    {
        // Remove instance numbers like " (2)" or "#2" and trim
        var trimmed = name.Trim();
        // Remove trailing instance identifiers
        trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\s*\(\d+\)$", "");
        trimmed = System.Text.RegularExpressions.Regex.Replace(trimmed, @"\s*#\d+$", "");
        return trimmed;
    }

    private static int CompareVersions(string? v1, string? v2)
    {
        if (string.IsNullOrWhiteSpace(v1) && string.IsNullOrWhiteSpace(v2)) return 0;
        if (string.IsNullOrWhiteSpace(v1)) return -1;
        if (string.IsNullOrWhiteSpace(v2)) return 1;
        // Simple lexical compare; real version compare would use VersionComparerService
        return string.Compare(v1, v2, StringComparison.OrdinalIgnoreCase);
    }
}
