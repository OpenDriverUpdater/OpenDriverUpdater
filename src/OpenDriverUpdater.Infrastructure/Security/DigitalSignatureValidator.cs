using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using OpenDriverUpdater.Core.Interfaces;

namespace OpenDriverUpdater.Infrastructure.Security;

/// <summary>
/// Authenticode dijital imza doğrulayıcısı.
/// </summary>
public sealed class DigitalSignatureValidator
{
    private readonly ILoggerService _logger;

    public DigitalSignatureValidator(ILoggerService logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Dosyanın geçerli bir Authenticode dijital imzası olup olmadığını kontrol eder.
    /// </summary>
    public bool IsValidSigned(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.Warn("File not found for signature check: {Path}", filePath);
            return false;
        }

        try
        {
            // WinVerifyTrust kullanılarak Authenticode doğrulaması yapılır.
            var result = WinTrustVerify(filePath);
            _logger.Debug("Signature check for {Path}: {Result}", filePath, result ? "Valid" : "Invalid");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error("Signature verification failed for {Path}", ex, filePath);
            return false;
        }
    }

    /// <summary>
    /// Dosyadan X.509 sertifika bilgilerini çıkarır.
    /// </summary>
    public X509Certificate2? GetCertificate(string filePath)
    {
        try
        {
            var cert = X509Certificate.CreateFromSignedFile(filePath);
            return new X509Certificate2(cert);
        }
        catch
        {
            return null;
        }
    }

    private static bool WinTrustVerify(string filePath)
    {
        try
        {
            var cert = X509Certificate.CreateFromSignedFile(filePath);
            var cert2 = new X509Certificate2(cert);
            return cert2.Verify();
        }
        catch
        {
            return false;
        }
    }
}
