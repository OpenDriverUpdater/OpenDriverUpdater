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

    private static object[] SanitizeArgs(object[] args)
    {
        if (args is null || args.Length == 0)
        {
            return Array.Empty<object>();
        }

        var sanitized = new object[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            sanitized[i] = args[i] switch
            {
                null => null!,
                string value => SanitizeString(value),
                _ => args[i]
            };
        }

        return sanitized;
    }

    private static string SanitizeString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "[REDACTED]";
        }

        if (LooksLikePath(value))
        {
            return SanitizePath(value);
        }

        return "[REDACTED]";
    }

    private static bool LooksLikePath(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && (Path.IsPathRooted(value)
                || value.Contains(Path.DirectorySeparatorChar)
                || value.Contains(Path.AltDirectorySeparatorChar));
    }

    private static string SanitizePath(string value)
    {
        var fileName = Path.GetFileName(value);
        return string.IsNullOrWhiteSpace(fileName) ? "[REDACTED]" : fileName;
    }
}