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
///
/// Çalıştırılabilir dosyalar PATH üzerinden değil, Windows sistem dizininden
/// (Environment.SystemDirectory) çözülür. Süreç başlatma <see cref="IProcessRunner"/>
/// arkasındadır; testler gerçek pnputil/expand çalıştırmaz.
/// </summary>
public sealed class PnpUtilInstaller : IInstaller
{
    // pnputil özel çıkış kodları
    private const int ExitSuccess = 0;
    private const int ExitRebootRequired = 3010;   // ERROR_SUCCESS_REBOOT_REQUIRED
    private const int ExitRebootInitiated = 1641;  // ERROR_SUCCESS_REBOOT_INITIATED

    private static readonly TimeSpan ExpandTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan PnPUtilTimeout = TimeSpan.FromMinutes(10);

    private readonly ILoggerService _logger;
    private readonly IProcessRunner _processRunner;
    private readonly ISignatureValidator _signatureValidator;

    public PnpUtilInstaller(ILoggerService logger)
        : this(
            logger,
            new SystemProcessRunner(),
            new OpenDriverUpdater.Infrastructure.Security.DigitalSignatureValidator(logger))
    {
    }

    public PnpUtilInstaller(ILoggerService logger, IProcessRunner processRunner)
        : this(
            logger,
            processRunner,
            new OpenDriverUpdater.Infrastructure.Security.DigitalSignatureValidator(logger))
    {
    }

    public PnpUtilInstaller(
        ILoggerService logger,
        IProcessRunner processRunner,
        ISignatureValidator signatureValidator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _signatureValidator = signatureValidator ?? throw new ArgumentNullException(nameof(signatureValidator));
    }

    public async Task<InstallResult> InstallAsync(
        DownloadedPackage package,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(0);

        if (!OperatingSystem.IsWindows())
        {
            return Fail(-1, "Driver installation with pnputil is only supported on Windows.");
        }

        if (!File.Exists(package.FilePath))
        {
            return Fail(-1, $"Package file not found: {package.FilePath}");
        }

        try
        {
            var pnputilPath = ResolveSystemTool("pnputil.exe");
            if (pnputilPath is null)
            {
                return Fail(-1, "pnputil.exe not found in the Windows system directory.");
            }

            // ── 1. Paket bir .cab ise önce aç ─────────────────────────────
            string installTarget;
            var isCab = Path.GetExtension(package.FilePath)
                .Equals(".cab", StringComparison.OrdinalIgnoreCase);

            if (isCab)
            {
                _logger.Info("Extracting driver package {Path}", package.FilePath);

                var expandPath = ResolveSystemTool("expand.exe");
                if (expandPath is null)
                {
                    return Fail(-1, "expand.exe not found in the Windows system directory.");
                }

                var extractDir = Path.Combine(
                    Path.GetDirectoryName(package.FilePath)!,
                    Path.GetFileNameWithoutExtension(package.FilePath));

                // Önceki denemeden kalan yarım çıkarma varsa temizle
                if (Directory.Exists(extractDir))
                    Directory.Delete(extractDir, recursive: true);
                Directory.CreateDirectory(extractDir);

                var (expandExit, expandOut, expandErr) = await _processRunner.RunAsync(
                    expandPath,
                    new[] { "-F:*", package.FilePath, extractDir },
                    ExpandTimeout,
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
                var signedFiles = Directory.GetFiles(extractDir, "*.*", SearchOption.AllDirectories)
                    .Where(f => f.EndsWith(".cat", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                // Fail-closed: katalog bulunamazsa imza kanıtlanamaz, kurulum reddedilir.
                if (signedFiles.Length == 0)
                {
                    return Fail(-1,
                        "No .cat catalog file was found in the extracted package, so no Authenticode " +
                        "signature could be validated. Installation refused.");
                }

                foreach (var file in signedFiles)
                {
                    if (!_signatureValidator.IsValidSigned(file))
                    {
                        return Fail(-1, $"Signature validation failed for {Path.GetFileName(file)}");
                    }
                }

                _logger.Info(
                    "Validated Authenticode signature of {Count} .cat file(s). " +
                    "This does not prove that the driver binaries match the catalog contents.",
                    signedFiles.Length);

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

            var (exitCode, stdout, stderr) = await _processRunner.RunAsync(
                pnputilPath,
                new[] { "/add-driver", installTarget, "/subdirs", "/install" },
                PnPUtilTimeout,
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

    private static string? ResolveSystemTool(string fileName)
    {
        var path = Path.Combine(Environment.SystemDirectory, fileName);
        return File.Exists(path) ? path : null;
    }

    private static InstallResult Fail(int exitCode, string message) => new()
    {
        Success = false,
        ExitCode = exitCode,
        Message = message,
        Status = InstallStatus.Failed
    };
}
