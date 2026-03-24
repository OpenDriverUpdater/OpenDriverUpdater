using System.Diagnostics;
using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Infrastructure.Install;

/// <summary>
/// pnputil tabanlı sürücü kurucu.
/// </summary>
public sealed class PnpUtilInstaller : IInstaller
{
    private readonly ILoggerService _logger;

    public PnpUtilInstaller(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<InstallResult> InstallAsync(
        DownloadedPackage package, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(package.FilePath))
        {
            return new InstallResult
            {
                Success = false,
                ExitCode = -1,
                Message = $"Package file not found: {package.FilePath}",
                Status = InstallStatus.Failed
            };
        }

        try
        {
            _logger.Info("Installing driver from {Path}", package.FilePath);

            var startInfo = new ProcessStartInfo
            {
                FileName = "pnputil",
                Arguments = $"/add-driver \"{package.FilePath}\" /install",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new InstallResult
                {
                    Success = false,
                    ExitCode = -1,
                    Message = "Failed to start pnputil process",
                    Status = InstallStatus.Failed
                };
            }

            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var requiresRestart = stdout.Contains("restart", StringComparison.OrdinalIgnoreCase);
            var success = process.ExitCode == 0;

            _logger.Info("pnputil exited with code {Code}. Output: {Output}", process.ExitCode, stdout);

            return new InstallResult
            {
                Success = success,
                ExitCode = process.ExitCode,
                Message = success ? stdout : $"{stdout}\n{stderr}".Trim(),
                RequiresRestart = requiresRestart,
                Status = success
                    ? (requiresRestart ? InstallStatus.RequiresRestart : InstallStatus.Success)
                    : InstallStatus.Failed
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Error("Driver installation failed", ex);
            return new InstallResult
            {
                Success = false,
                ExitCode = -1,
                Message = ex.Message,
                Status = InstallStatus.Failed
            };
        }
    }
}
