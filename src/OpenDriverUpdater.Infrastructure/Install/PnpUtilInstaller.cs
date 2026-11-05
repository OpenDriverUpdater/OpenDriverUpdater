using System.Diagnostics;
using System.Linq;
using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Interfaces;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Infrastructure.Install;

/// <summary>
/// pnputil tabanlı sürücü kurucu. .cab paketlerini önce expand.exe ile açar,
/// ardından içindeki INF'leri pnputil /add-driver /install ile kurar.
/// NOT: pnputil /install yönetici yetkisi gerektirir — uygulama yükseltilmiş
/// çalışmıyorsa kurulum "Access denied" ile başarısız olur.
/// </summary>
public sealed class PnpUtilInstaller : IInstaller
{
    // pnputil özel çıkış kodları
    private const int ExitSuccess = 0;
    private const int ExitRebootRequired = 3010;   // ERROR_SUCCESS_REBOOT_REQUIRED
    private const int ExitRebootInitiated = 1641;  // ERROR_SUCCESS_REBOOT_INITIATED

    private readonly ILoggerService _logger;

    public PnpUtilInstaller(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<InstallResult> InstallAsync(
        DownloadedPackage package,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(0);
        if (!File.Exists(package.FilePath))
        {
            return Fail(-1, $"Package file not found: {package.FilePath}");
        }

        try
        {
            // ── 1. Paket bir .cab ise önce aç ─────────────────────────────
            string installTarget;
            var isCab = Path.GetExtension(package.FilePath)
                .Equals(".cab", StringComparison.OrdinalIgnoreCase);

            if (isCab)
            {
                _logger.Info("Extracting driver package {Path}", package.FilePath);

                var extractDir = Path.Combine(
                    Path.GetDirectoryName(package.FilePath)!,
                    Path.GetFileNameWithoutExtension(package.FilePath));

                // Önceki denemeden kalan yarım çıkarma varsa temizle
                if (Directory.Exists(extractDir))
                    Directory.Delete(extractDir, recursive: true);
                Directory.CreateDirectory(extractDir);

                var (expandExit, expandOut, expandErr) = await RunProcessAsync(
                    "expand.exe",
                    $"-F:* \"{package.FilePath}\" \"{extractDir}\"",
                    cancellationToken);

                if (expandExit != 0)
                {
                    return Fail(expandExit,
                        $"Package extraction failed: {expandOut}\n{expandErr}".Trim());
                }

                var infFiles = Directory.GetFiles(extractDir, "*.inf", SearchOption.AllDirectories);
                if (infFiles.Length == 0)
                {
                    return Fail(-1, "No INF file found in extracted package");
                }

                _logger.Info("Extracted {Count} INF file(s) from package", infFiles.Length);

                // Phase 3: Authenticode validation
                var validator = new OpenDriverUpdater.Infrastructure.Security.DigitalSignatureValidator(_logger);
                var signedFiles = Directory.GetFiles(extractDir, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".cat", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                foreach (var file in signedFiles)
                {
                    if (!validator.IsValidSigned(file))
                    {
                        return Fail(-1, $"Signature validation failed for {Path.GetFileName(file)}");
                    }
                }

                _logger.Info("Signature validation passed for {Count} file(s)", signedFiles.Length);

                // Tek pnputil çağrısı: dizindeki tüm INF'ler, alt dizinler dahil
                installTarget = Path.Combine(extractDir, "*.inf");
            }
            else
            {
                // .inf (veya pnputil'in doğrudan kabul ettiği başka bir format)
                installTarget = package.FilePath;
            }

            progress?.Report(0.3);

            // ── 2. pnputil ile kur ────────────────────────────────────────
            _logger.Info("Installing driver from {Target}", installTarget);

            var (exitCode, stdout, stderr) = await RunProcessAsync(
                "pnputil",
                $"/add-driver \"{installTarget}\" /subdirs /install",
                cancellationToken);

            // pnputil çoklu paketlerde herhangi biri başarısız olursa sıfır-dışı
            // çıkış kodu döner. Gerçek sonucu çıktıdaki sayaçlardan oku:
            // kısmi başarı (en az bir paket eklendi) başarı sayılır.
            var totalMatch = System.Text.RegularExpressions.Regex.Match(
                stdout, @"Total driver packages:\s*(\d+)");
            var addedMatch = System.Text.RegularExpressions.Regex.Match(
                stdout, @"Added driver packages:\s*(\d+)");

            var totalPackages = totalMatch.Success ? int.Parse(totalMatch.Groups[1].Value) : 0;
            var addedPackages = addedMatch.Success ? int.Parse(addedMatch.Groups[1].Value) : 0;

            var requiresRestart =
                exitCode is ExitRebootRequired or ExitRebootInitiated ||
                stdout.Contains("restart", StringComparison.OrdinalIgnoreCase);

            var success =
                exitCode is ExitSuccess or ExitRebootRequired or ExitRebootInitiated ||
                addedPackages > 0;

            _logger.Info("pnputil exited with code {Code}: {Added}/{Total} packages added",
                exitCode, addedPackages, totalPackages);

            _logger.Debug("pnputil output: {Output}", stdout);

            progress?.Report(1.0);

            return new InstallResult
            {
                Success = success,
                ExitCode = exitCode,
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
            return Fail(-1, ex.Message);
        }
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(
        string fileName, string arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {fileName}");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, await stdoutTask, await stderrTask);
    }

    private static InstallResult Fail(int exitCode, string message) => new()
    {
        Success = false,
        ExitCode = exitCode,
        Message = message,
        Status = InstallStatus.Failed
    };
}