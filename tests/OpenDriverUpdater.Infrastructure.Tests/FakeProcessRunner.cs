using OpenDriverUpdater.Infrastructure.Install;

namespace OpenDriverUpdater.Infrastructure.Tests;

internal sealed class FakeProcessRunner : IProcessRunner
{
    private readonly Func<string, IReadOnlyList<string>, TimeSpan?, CancellationToken, Task<ProcessRunResult>> _handler;

    public FakeProcessRunner(
        Func<string, IReadOnlyList<string>, TimeSpan?, CancellationToken, Task<ProcessRunResult>> handler)
    {
        _handler = handler;
    }

    public List<ProcessCall> Calls { get; } = new();

    public Task<ProcessRunResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        Calls.Add(new ProcessCall(fileName, arguments.ToArray(), timeout));
        return _handler(fileName, arguments, timeout, cancellationToken);
    }
}

internal sealed record ProcessCall(
    string FileName,
    IReadOnlyList<string> Arguments,
    TimeSpan? Timeout);
