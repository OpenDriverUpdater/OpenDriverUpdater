namespace OpenDriverUpdater.Core.Interfaces;

/// <summary>
/// Dosyanın Authenticode imzasının doğrulanıp doğrulanamayacağını soyutlar.
/// Kurulum akışı, test edilebilirlik için doğrulamayı bu arayüz üzerinden alır.
/// Desteklenmeyen platformlarda ve hata durumunda implementasyon <b>fail-closed</b>
/// olarak <c>false</c> dönmelidir; imza doğrulanamıyorsa dosya doğrulanmış sayılmaz.
/// </summary>
public interface ISignatureValidator
{
    bool IsValidSigned(string filePath);
}
