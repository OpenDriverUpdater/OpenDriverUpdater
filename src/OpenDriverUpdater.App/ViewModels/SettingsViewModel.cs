using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenDriverUpdater.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _microsoftCatalogEnabled = true;
    [ObservableProperty] private bool _intelEnabled = true;
    [ObservableProperty] private bool _amdEnabled = true;
    [ObservableProperty] private bool _nvidiaEnabled = true;

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
