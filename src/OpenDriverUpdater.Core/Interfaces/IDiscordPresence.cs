namespace OpenDriverUpdater.Core.Interfaces;

/// <summary>
/// Discord Rich Presence (RPC) durumu.
///
/// Bu hizmet, kullanıcının Discord profilinde "OpenDriverUpdater ile neyle ilgili işlemde"
/// bilgisini gösterir. İsteğe bağlıdır ve varsayılan olarak kapalıdır (gizlilik politikası).
/// Discord istemcisi yoksa veya etkinleştirilmezse fail-safe olarak hiçbir işlem yapmaz.
/// </summary>
public interface IDiscordPresence
{
    /// <summary>Discord RPC etkinleştirilmiş mi?</summary>
    bool IsEnabled { get; set; }

    /// <summary>Mevcut durumu günceller.</summary>
    /// <param name="details">Ana ayrıntı (örn. "Sürücü taranıyor...").</param>
    /// <param name="state">İkincil durum (örn. "Intel güncellemesi yükleniyor").</param>
    void SetPresence(string details, string state);

    /// <summary>Discord durumunu temizler.</summary>
    void ClearPresence();

    void Dispose();
}
