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

    public void Info(string message, params object[] args) => _logger.Information(message, args);
    public void Warn(string message, params object[] args) => _logger.Warning(message, args);
    public void Error(string message, Exception? ex = null, params object[] args)
    {
        if (ex is not null)
            _logger.Error(ex, message, args);
        else
            _logger.Error(message, args);
    }
    public void Debug(string message, params object[] args) => _logger.Debug(message, args);
}
