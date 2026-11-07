using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using OpenDriverUpdater.Core.Interfaces;

namespace OpenDriverUpdater.Infrastructure.Security;

/// <summary>
/// Authenticode dosya imzası doğrulayıcısı.
///
/// Bu sınıf yalnızca "dosyanın kendisi geçerli bir Authenticode imzası taşıyor mu?"
/// sorusunu yanıtlar. Sonuç; sürücünün donanımla uyumlu olduğunu, Microsoft
/// tarafından yayımlandığını veya paket içindeki her dosyanın imzalı olduğunu
/// KANITLAMAZ. Katalog (.cat) imzası ile INF/SYS dosyalarının doğrudan imzası
/// ayrı kavramlardır; çağıran taraf bunları karıştırmamalıdır.
///
/// Windows dışında doğrulama yapılamaz ve metot fail-closed olarak false döner.
/// Ağ/CRL/OCSP erişimi başarısız olduğunda da WinVerifyTrust hata döndürür ve
/// bu sınıf imzayı geçerli saymaz.
/// </summary>
public sealed class DigitalSignatureValidator : ISignatureValidator
{
    private readonly ILoggerService _logger;
    private readonly bool _isWindows;

    public DigitalSignatureValidator(ILoggerService logger)
        : this(logger, RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
    }

    /// <summary>
    /// Testlerde platform dalını doğrulamak için kullanılan dahili kurucu.
    /// Üretimde daima <see cref="DigitalSignatureValidator(ILoggerService)"/> kullanılır.
    /// </summary>
    internal DigitalSignatureValidator(ILoggerService logger, bool isWindows)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _isWindows = isWindows;
    }

    /// <summary>
    /// Dosyanın geçerli bir Authenticode dijital imzası olup olmadığını kontrol eder.
    /// Yalnızca WinVerifyTrust S_OK (0) döndürdüğünde true döner.
    /// </summary>
    public bool IsValidSigned(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.Warn("Signature check called with an empty path");
            return false;
        }

        if (!_isWindows)
        {
            _logger.Warn(
                "Authenticode verification is not supported on this platform; refusing to treat {Path} as verified",
                filePath);
            return false;
        }

        if (!File.Exists(filePath))
        {
            _logger.Warn("File not found for signature check: {Path}", filePath);
            return false;
        }

        try
        {
            var valid = WinTrustVerify(filePath, out var status);
            if (valid)
            {
                _logger.Debug("Authenticode signature valid for {Path}", filePath);
            }
            else
            {
                _logger.Warn(
                    "Authenticode signature invalid for {Path} (WinVerifyTrust status 0x{Status:X8})",
                    filePath,
                    status);
            }

            return valid;
        }
        catch (Exception ex)
        {
            _logger.Error("Signature verification failed for {Path}", ex, filePath);
            return false;
        }
    }

    /// <summary>
    /// Dosyadan imzalayan sertifikayı çıkarır. Bu metot imzayı DOĞRULAMAZ;
    /// yalnızca görüntüleme/telemetri amaçlıdır. Güven kararı için
    /// <see cref="IsValidSigned(string)"/> kullanılmalıdır.
    /// </summary>
    public X509Certificate2? GetCertificate(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

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

    /// <summary>
    /// WinVerifyTrust ile Authenticode doğrulaması yapar.
    ///
    /// Politika:
    /// - Yalnızca S_OK (0) başarı kabul edilir; diğer tüm HRESULT değerleri başarısızdır.
    /// - Sertifika zinciri ve iptal kontrolü (root hariç tüm zincir) etkindir:
    ///   fdwRevocationChecks = WTD_REVOKE_NONE ve
    ///   dwProvFlags = WTD_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT.
    ///   Ağ/CRL/OCSP erişilemezse WinVerifyTrust hata döndürür; bu durumda dosya
    ///   doğrulanmış sayılmaz (fail-closed).
    /// - Zaman damgası, WinVerifyTrust'ın sistem politikasına bırakılır. Zaman damgası
    ///   yoksa imza mevcut zamanda geçerli olmalıdır; zaman damgası varsa imzanın
    ///   damga anında geçerli olması yeterlidir. Bu sınıf bu politikayı atlatmaz.
    /// - WTD_STATEACTION_VERIFY sonrası WTD_STATEACTION_CLOSE çağrılarak
    ///   WinTrust durum verisi serbest bırakılır.
    /// </summary>
    private static bool WinTrustVerify(string filePath, out int status)
    {
        const uint WTD_UI_NONE = 2;
        const uint WTD_REVOKE_NONE = 0;
        const uint WTD_CHOICE_FILE = 1;
        const uint WTD_STATEACTION_VERIFY = 1;
        const uint WTD_STATEACTION_CLOSE = 2;
        const uint WTD_UICONTEXT_INSTALL = 1;
        const uint WTD_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT = 0x00000080;

        var fullPath = Path.GetFullPath(filePath);
        var nativePath = IntPtr.Zero;
        var fileInfoPtr = IntPtr.Zero;
        status = unchecked((int)0x80004005); // E_FAIL varsayılanı

        try
        {
            nativePath = Marshal.StringToHGlobalUni(fullPath);
            fileInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_FILE_INFO>());

            var fileInfo = new WINTRUST_FILE_INFO
            {
                cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(),
                pcwszFilePath = nativePath,
                hFile = IntPtr.Zero,
                pgKnownSubject = IntPtr.Zero,
            };
            Marshal.StructureToPtr(fileInfo, fileInfoPtr, false);

            var data = new WINTRUST_DATA
            {
                cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
                pPolicyCallbackData = IntPtr.Zero,
                pSIPClientData = IntPtr.Zero,
                dwUIChoice = WTD_UI_NONE,
                fdwRevocationChecks = WTD_REVOKE_NONE,
                dwUnionChoice = WTD_CHOICE_FILE,
                pFile = fileInfoPtr,
                dwStateAction = WTD_STATEACTION_VERIFY,
                hWVTStateData = IntPtr.Zero,
                pwszURLReference = IntPtr.Zero,
                dwProvFlags = WTD_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT,
                dwUIContext = WTD_UICONTEXT_INSTALL,
                pSignatureSettings = IntPtr.Zero,
            };

            // WINTRUST_ACTION_GENERIC_VERIFY_V2
            var action = new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");
            status = WinVerifyTrust(IntPtr.Zero, ref action, ref data);
            var valid = status == 0;

            // WinVerifyTrust'ın ayırdığı durum verisini serbest bırak.
            data.dwStateAction = WTD_STATEACTION_CLOSE;
            _ = WinVerifyTrust(IntPtr.Zero, ref action, ref data);

            return valid;
        }
        finally
        {
            if (fileInfoPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(fileInfoPtr);
            }

            if (nativePath != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(nativePath);
            }
        }
    }

    /// <summary>
    /// Testlerde P/Invoke yapı boyutlarının Windows SDK yerleşimiyle uyumlu
    /// olduğunu doğrulamak için kullanılır.
    /// </summary>
    internal static (int FileInfoSize, int DataSize) GetNativeStructSizes()
        => (Marshal.SizeOf<WINTRUST_FILE_INFO>(), Marshal.SizeOf<WINTRUST_DATA>());

    [StructLayout(LayoutKind.Sequential)]
    private struct WINTRUST_FILE_INFO
    {
        public uint cbStruct;
        public IntPtr pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    /// <summary>
    /// Windows SDK WINTRUST_DATA. pFile alanı, dwUnionChoice'den sonra gelen
    /// union'ın dosya üyesidir; yapı sırası SDK tanımıyla birebir aynı olmalıdır.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct WINTRUST_DATA
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pFile;
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        public IntPtr pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
        public IntPtr pSignatureSettings;
    }

    [DllImport("wintrust.dll", SetLastError = true)]
    private static extern int WinVerifyTrust(IntPtr hwnd, ref Guid pgActionID, ref WINTRUST_DATA pWVTData);
}
