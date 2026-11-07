namespace OpenDriverUpdater.Infrastructure.Tests;

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory(string prefix = "odu test")
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "odu-tests",
            $"{prefix}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string File(string fileName) => System.IO.Path.Combine(Path, fileName);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Test temizliği başarısız olsa da asıl test sonucu korunur.
        }
    }
}
