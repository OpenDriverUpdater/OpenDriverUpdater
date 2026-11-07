using System.Runtime.InteropServices;
using System.Security.Cryptography;
using OpenDriverUpdater.Infrastructure.Security;
using Xunit;

namespace OpenDriverUpdater.Infrastructure.Tests;

public sealed class DigitalSignatureValidatorTests
{
    private readonly TestLogger _logger = new();

    /// <summary>
    /// .NET kurulumundaki dotnet.exe, Microsoft tarafından gömülü (embedded)
    /// Authenticode imzasıyla imzalanmıştır. Sistemdeki birçok ikili gibi
    /// yalnızca katalog imzası taşımadığı için WinVerifyTrust WTD_CHOICE_FILE
    /// ile doğrulanabilir.
    /// </summary>
    private static string TrustedEmbeddedSignedBinary()
    {
        var dotnetRoot = Path.GetFullPath(
            Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", ".."));
        return Path.Combine(dotnetRoot, "dotnet.exe");
    }

    [Fact]
    public void WinTrustStructs_HaveExpected64BitLayout()
    {
        // Bu test projesi 64-bit Windows'ta çalışır. 32-bit hedef eklenirse
        // yapı boyutları için ayrı bir beklenti tanımlanmalıdır.
        Assert.Equal(8, IntPtr.Size);

        var (fileInfoSize, dataSize) = DigitalSignatureValidator.GetNativeStructSizes();

        Assert.Equal(32, fileInfoSize);
        Assert.Equal(88, dataSize);
    }

    [Fact]
    public void IsValidSigned_MissingFile_ReturnsFalse()
    {
        using var temp = new TempDirectory();
        var validator = new DigitalSignatureValidator(_logger);

        var result = validator.IsValidSigned(temp.File("does-not-exist.sys"));

        Assert.False(result);
        Assert.Contains(_logger.Messages, m => m.Contains("File not found", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void IsValidSigned_UnsignedFile_ReturnsFalse()
    {
        using var temp = new TempDirectory();
        var path = temp.File("unsigned.sys");
        File.WriteAllText(path, "this is not a signed PE file");
        var validator = new DigitalSignatureValidator(_logger);

        Assert.False(validator.IsValidSigned(path));
    }

    [Fact]
    public void IsValidSigned_CorruptPeFile_ReturnsFalse()
    {
        using var temp = new TempDirectory();
        var path = temp.File("corrupt.sys");
        File.WriteAllBytes(path, RandomNumberGenerator.GetBytes(1024));
        var validator = new DigitalSignatureValidator(_logger);

        Assert.False(validator.IsValidSigned(path));
    }

    [Fact]
    public void IsValidSigned_TamperedSignedFile_ReturnsFalse()
    {
        var source = TrustedEmbeddedSignedBinary();
        Assert.True(File.Exists(source), $"Embedded-signed binary not found: {source}");

        using var temp = new TempDirectory();
        var tampered = temp.File("tampered.exe");
        File.Copy(source, tampered);

        // İmza karmasını bozacak şekilde gövdeden bir bayt değiştir.
        var bytes = File.ReadAllBytes(tampered);
        bytes[bytes.Length / 2] ^= 0xFF;
        File.WriteAllBytes(tampered, bytes);

        var validator = new DigitalSignatureValidator(_logger);

        Assert.False(validator.IsValidSigned(tampered));
    }

    [Fact]
    public void IsValidSigned_TrustedSignedSystemBinary_ReturnsTrue()
    {
        var source = TrustedEmbeddedSignedBinary();
        Assert.True(File.Exists(source), $"Embedded-signed binary not found: {source}");

        var validator = new DigitalSignatureValidator(_logger);

        Assert.True(
            validator.IsValidSigned(source),
            "WinVerifyTrust did not accept the trusted Microsoft-signed system binary. " +
            "This can happen offline if revocation information cannot be retrieved; " +
            "the validator is intentionally fail-closed. Log: " +
            string.Join(" | ", _logger.Messages));
    }

    private static string? FindFirstCatalog(string root)
    {
        if (!Directory.Exists(root))
        {
            return null;
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
        };
        return Directory.EnumerateFiles(root, "*.cat", options).FirstOrDefault();
    }

    [Fact]
    public void IsValidSigned_SignedCatalogFile_ReturnsTrue()
    {
        var systemDirectory = Environment.SystemDirectory;
        var catalog =
            FindFirstCatalog(Path.Combine(systemDirectory, "DriverStore", "FileRepository")) ??
            FindFirstCatalog(Path.Combine(systemDirectory, "CatRoot"));

        Assert.False(string.IsNullOrEmpty(catalog), "No signed .cat file found for the catalog integration test.");

        var validator = new DigitalSignatureValidator(_logger);

        Assert.True(
            validator.IsValidSigned(catalog!),
            $"WinVerifyTrust did not accept the catalog file: {catalog}. Log: " +
            string.Join(" | ", _logger.Messages));
    }

    [Fact]
    public void IsValidSigned_NonWindowsSimulation_ReturnsFalse()
    {
        using var temp = new TempDirectory();
        var path = temp.File("any.sys");
        File.WriteAllText(path, "content");

        var validator = new DigitalSignatureValidator(_logger, isWindows: false);

        Assert.False(validator.IsValidSigned(path));
        Assert.Contains(
            _logger.Messages,
            m => m.Contains("not supported on this platform", StringComparison.OrdinalIgnoreCase));
    }
}
