using System.Diagnostics;
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

            devices.Add(new DeviceInfo
            {
                Id = instanceId,
                Name = dict.GetValueOrDefault("Device Description", "Unknown Device"),
                Class = DeviceClass.Unknown,
                Manufacturer = dict.GetValueOrDefault("Manufacturer"),
                HardwareIds = string.IsNullOrWhiteSpace(instanceId) ? [] : [instanceId],
                InstalledDriverVersion = dict.GetValueOrDefault("Driver Version"),
                InstalledProvider = dict.GetValueOrDefault("Driver Provider Name")
            });
        }

        return devices;
    }
}
