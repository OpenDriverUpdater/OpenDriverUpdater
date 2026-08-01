#if WINDOWS
using System.Management;
#endif
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

            _logger.Info("WMI scan completed: {Count} devices found", devices.Count);
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
}
