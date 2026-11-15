#if WINDOWS
using System.Management;
#endif
using OpenDriverUpdater.Core.Interfaces;

namespace OpenDriverUpdater.Infrastructure.Safety;

/// <summary>
/// Creates a System Restore point before a driver install.
///
/// Calls the WMI method root\default:SystemRestore.CreateRestorePoint
/// directly. This is what PowerShell's Checkpoint-Computer does under the
/// hood, but without spawning powershell.exe with a command line, a
/// pattern antivirus behaviour monitors (e.g. Norton IDP.*) flag heavily.
/// </summary>
public sealed class SystemSafetyService
{
    // SystemRestore.CreateRestorePoint constants (srrestoreptapi.h)
    private const uint ModifySettings = 12;        // RestorePointType: same type Checkpoint-Computer used here
    private const uint BeginSystemChange = 100;    // EventType

    private readonly ILoggerService _logger;

    public SystemSafetyService(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <returns>
    /// True if Windows accepted the request. Note Windows allows one new
    /// restore point per 24 hours by default and may quietly skip extra ones;
    /// it also fails if System Protection is turned off for the system drive.
    /// </returns>
    public Task<bool> CreateRestorePointAsync(string description, CancellationToken ct = default)
    {
#if WINDOWS
        // WMI calls block; keep them off the UI thread.
        return Task.Run(() => CreateRestorePoint(description, ct), ct);
#else
        _logger.Warn("Restore points are only supported on Windows");
        return Task.FromResult(false);
#endif
    }

#if WINDOWS
    private bool CreateRestorePoint(string description, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // Windows limits the description to 256 characters.
        if (description.Length > 256)
            description = description[..256];

        try
        {
            var scope = new ManagementScope(@"\\.\root\default");
            using var restore = new ManagementClass(scope, new ManagementPath("SystemRestore"), null);
            using var input = restore.GetMethodParameters("CreateRestorePoint");
            input["Description"] = description;
            input["RestorePointType"] = ModifySettings;
            input["EventType"] = BeginSystemChange;

            using var output = restore.InvokeMethod("CreateRestorePoint", input, null);
            var code = Convert.ToUInt32(output?["ReturnValue"] ?? uint.MaxValue);

            if (code == 0)
            {
                _logger.Info("System restore point requested: {Desc}", description);
                return true;
            }

            // Usually System Protection is off for the system drive, or the
            // System Restore service is disabled.
            _logger.Warn("Restore point creation failed with code {Code} (0x{Hex:X8}). Is System Protection turned on for the system drive?",
                code, code);
            return false;
        }
        catch (ManagementException ex)
        {
            _logger.Warn("Restore point creation failed: {Error}", ex.Message);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Error("Restore point creation exception", ex);
            return false;
        }
    }
#endif
}
