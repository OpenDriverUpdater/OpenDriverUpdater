using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenDriverUpdater.Core.Interfaces;

namespace OpenDriverUpdater.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _microsoftCatalogEnabled = true;
    [ObservableProperty] private bool _intelEnabled = true;
    [ObservableProperty] private bool _amdEnabled = true;
    [ObservableProperty] private bool _nvidiaEnabled = true;

    /// <summary>
    /// Discord Rich Presence (varlık durumu) etkin mi?
    /// Varsayılan kapalı; ODU_DISCORD_APP_ID ayarlandığında açılebilir.
    /// </summary>
    [ObservableProperty]
    private bool _discordRpcEnabled;

    public SettingsViewModel()
    {
        // Uygulama başlangıcındaki durumu yansıt.
        _discordRpcEnabled = App.DiscordPresence.IsEnabled;
        OnPropertyChanged(nameof(DiscordRpcEnabled));
    }

    partial void OnDiscordRpcEnabledChanged(bool value)
    {
        App.DiscordPresence.IsEnabled = value;
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        try
        {
            await App.MetadataStore.ClearCacheAsync();
            App.Logger.Info("Cache cleared from settings");
        }
        catch (Exception ex)
        {
            App.Logger.Error("Cache clear failed", ex);
        }
    }
}
