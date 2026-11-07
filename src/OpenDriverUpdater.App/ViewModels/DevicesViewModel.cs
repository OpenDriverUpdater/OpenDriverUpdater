using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenDriverUpdater.Core.Enums;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.App.ViewModels;

public partial class DevicesViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<DeviceInfo> Devices { get; } = [];

    public DevicesViewModel()
    {
        _ = RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            Devices.Clear();

            App.DiscordPresence.SetPresence("Donanım taranıyor...", "OpenDriverUpdater");

            var devices = await App.Scanner.ScanAsync();
            var ordered = devices.OrderByDescending(GetPriority).ThenBy(d => d.Class).ThenBy(d => d.Name).ToList();
            foreach (var device in ordered)
                Devices.Add(device);

            App.DiscordPresence.SetPresence($"{devices.Count} cihaz bulundu", "Hazır");
        }
        catch (Exception ex)
        {
            App.Logger.Error("Device refresh failed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static int GetPriority(DeviceInfo d)
    {
        return d.Class switch
        {
            DeviceClass.Display => 100,
            DeviceClass.Network => 90,
            DeviceClass.Audio => 80,
            DeviceClass.Storage => 70,
            DeviceClass.USB => 50,
            DeviceClass.Input => 40,
            _ => 10
        };
    }
}
