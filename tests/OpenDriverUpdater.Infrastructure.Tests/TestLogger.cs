using OpenDriverUpdater.Core.Interfaces;

namespace OpenDriverUpdater.Infrastructure.Tests;

internal sealed class TestLogger : ILoggerService
{
    public List<string> Messages { get; } = new();

    public void Info(string message, params object[] args) => Add("INFO", message, args);

    public void Warn(string message, params object[] args) => Add("WARN", message, args);

    public void Error(string message, Exception? ex = null, params object[] args)
    {
        Add("ERROR", message, args);
        if (ex is not null)
        {
            Messages.Add($"ERROR: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void Debug(string message, params object[] args) => Add("DEBUG", message, args);

    private void Add(string level, string message, object[] args)
    {
        Messages.Add(args.Length == 0
            ? $"{level}: {message}"
            : $"{level}: {message} | {string.Join(", ", args)}");
    }
}
