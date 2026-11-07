using System.Diagnostics;

namespace OpenDriverUpdater.Infrastructure.Install;

/// <summary>
/// <see cref="IProcessRunner"/> üretim uygulaması.
///
/// Komut satırı tek bir string olarak birleştirilmez; her argüman
/// <see cref="ProcessStartInfo.ArgumentList"/>'e ayrı ayrı eklenir. Böylece
/// boşluk, tırnak ve Unicode içeren yollar kabuk tarafından yeniden
/// yorumlanmaz. Kabuk (cmd.exe/PowerShell) kullanılmaz.
/// </summary>
public sealed class SystemProcessRunner : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Executable path must not be empty.", nameof(fileName));
        }

        ArgumentNullException.ThrowIfNull(arguments);

        var startInfo = CreateStartInfo(fileName, arguments);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start process: {fileName}");

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        if (timeout is { } timeoutValue)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeoutValue);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);

                if (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        $"Process '{fileName}' timed out after {timeoutValue}.");
                }

                throw;
            }
        }
        else
        {
            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }
        }

        var stdOut = await stdoutTask.ConfigureAwait(false);
        var stdErr = await stderrTask.ConfigureAwait(false);

        return new ProcessRunResult(process.ExitCode, stdOut, stdErr);
    }

    /// <summary>
    /// Testlerde ArgumentList kullanımını gerçek süreç başlatmadan doğrulamak
    /// için ayrılmıştır.
    /// </summary>
    internal static ProcessStartInfo CreateStartInfo(
        string fileName,
        IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Süreç zaten çıkmış veya erişim reddedilmiş olabilir. Asıl hata
            // çağırana iletilir; temizlik hatası onu gölgelememeli.
        }
    }
}
