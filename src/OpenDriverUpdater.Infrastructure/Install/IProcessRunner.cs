namespace OpenDriverUpdater.Infrastructure.Install;

/// <summary>
/// Bir alt sürecin sonucu. Standart çıktı ve hata, çağıranın karar vermesi için
/// ayrı tutulur.
/// </summary>
public sealed record ProcessRunResult(int ExitCode, string StdOut, string StdErr);

/// <summary>
/// Süreç başlatmayı test edilebilir hale getiren soyutlama. Üretimde
/// <see cref="SystemProcessRunner"/>, testlerde sahte bir uygulama kullanılır.
/// Gerçek pnputil/expand çağrıları testlerde çalıştırılmaz.
/// </summary>
public interface IProcessRunner
{
    Task<ProcessRunResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        TimeSpan? timeout,
        CancellationToken cancellationToken);
}
