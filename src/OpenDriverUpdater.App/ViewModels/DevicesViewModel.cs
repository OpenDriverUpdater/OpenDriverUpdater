using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

            var devices = await App.Scanner.ScanAsync();
            foreach (var device in devices)
                Devices.Add(device);
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
}
