using OpenDriverUpdater.Core.Interfaces;
using Serilog;

namespace OpenDriverUpdater.Infrastructure.Logging;

/// <summary>
/// Serilog tabanlı loglama servisi.
/// </summary>
public sealed class SerilogLoggerService : ILoggerService
{
    private readonly ILogger _logger;

    public SerilogLoggerService(string logDirectory)
    {
        Directory.CreateDirectory(logDirectory);

        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logDirectory, "odu-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private static object[] SanitizeArgs(object[] args)
    {
        if (args.Length == 0)
        {
            return args;
        }

        var sanitized = new object[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] is string s && (s.Contains(Path.DirectorySeparatorChar) || s.Contains(Path.AltDirectorySeparatorChar)))
            {
                sanitized[i] = "[REDACTED_PATH]";
            }
            else
            {
                sanitized[i] = args[i];
            }
        }

        return sanitized;
    }

    public void Info(string message, params object[] args) => _logger.Information(message, SanitizeArgs(args));
    public void Warn(string message, params object[] args) => _logger.Warning(message, SanitizeArgs(args));
    public void Error(string message, Exception? ex = null, params object[] args)
    {
        var sanitizedArgs = SanitizeArgs(args);
        if (ex is not null)
            _logger.Error(ex, message, sanitizedArgs);
        else
            _logger.Error(message, sanitizedArgs);
    }
    public void Debug(string message, params object[] args) => _logger.Debug(message, SanitizeArgs(args));
}
