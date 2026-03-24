using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.Core.Rules;

/// <summary>
/// Sürücü güncelleme kuralları — hangi sürücülerin önerilip önerilmeyeceğini belirler.
/// </summary>
public static class UpdateRules
{
    /// <summary>
    /// Aday sürücü önerilmeli mi?
    /// </summary>
    public static bool ShouldRecommend(CandidateDriver candidate, TrustScore trust)
    {
        // Blacklisted sürücüler asla önerilmez
        if (trust.Level == TrustLevel.Blacklisted)
            return false;

        // İmzasız sürücüler varsayılan olarak önerilmez
        if (!candidate.IsSigned && trust.Level < TrustLevel.Trusted)
            return false;

        // Beta sürücüler sadece güvenilir kaynaklardan önerilir
        if (candidate.IsBeta && trust.Level < TrustLevel.Trusted)
            return false;

        return true;
    }

    /// <summary>
    /// Otomatik kuruluma izin verilmeli mi?
    /// </summary>
    public static bool AllowAutoInstall(CandidateDriver candidate, TrustScore trust)
    {
        // Sadece Verified seviyesinde ve imzalı sürücüler otomatik kurulabilir
        return trust.Level == TrustLevel.Verified
            && candidate.IsSigned
            && !candidate.IsBeta;
    }

    /// <summary>
    /// Sürücü kaynağı bu cihaz sınıfı için geçerli mi?
    /// </summary>
    public static bool IsSourceValidForDevice(SourceType source, DeviceClass deviceClass)
    {
        return (source, deviceClass) switch
        {
            (SourceType.Nvidia, DeviceClass.Display) => true,
            (SourceType.Nvidia, _) => false, // NVIDIA sadece GPU sürücüsü sunar

            (SourceType.Intel, DeviceClass.Display) => true,
            (SourceType.Intel, DeviceClass.Network) => true,
            (SourceType.Intel, DeviceClass.Bluetooth) => true,
            (SourceType.Intel, DeviceClass.Storage) => true,
            (SourceType.Intel, _) => false,

            (SourceType.Amd, DeviceClass.Display) => true,
            (SourceType.Amd, _) => false,

            (SourceType.MicrosoftCatalog, _) => true, // Microsoft Catalog her sınıfı destekler
            _ => true,
        };
    }
}
