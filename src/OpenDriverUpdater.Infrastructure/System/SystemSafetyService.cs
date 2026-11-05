using System.Diagnostics;
using OpenDriverUpdater.Core.Interfaces;

namespace OpenDriverUpdater.Infrastructure.Safety;

public sealed class SystemSafetyService
{
    private readonly ILoggerService _logger;

    public SystemSafetyService(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> CreateRestorePointAsync(string description, CancellationToken ct = default)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-NoProfile -Command \"Checkpoint-Computer -Description '{description}' -RestorePointType 'MODIFY_SETTINGS'\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi);
            if (proc == null)
            {
                _logger.Warn("Failed to start PowerShell for restore point");
                return false;
            }

            var stdout = await proc.StandardOutput.ReadToEndAsync(ct);
            var stderr = await proc.StandardError.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);

            if (proc.ExitCode == 0)
            {
                _logger.Info("System restore point created: {Desc}", description);
                return true;
            }

            _logger.Warn("Restore point creation failed: {Error}", stderr);
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error("Restore point creation exception", ex);
            return false;
        }
    }
}
