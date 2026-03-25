using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenDriverUpdater.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private int _deviceCount;

    [ObservableProperty]
    private int _updateCount;

    [ObservableProperty]
    private string _lastScanText = "Never";

    [ObservableProperty]
    private string _systemHealthStatus = "Unknown";

    [ObservableProperty]
    private string _statusBrush = "TextBrush";

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private async Task ScanAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Scanning devices...";

            var devices = await App.Scanner.ScanAsync();
            DeviceCount = devices.Count;
            LastScanText = DateTime.Now.ToString("HH:mm:ss");
            
            SystemHealthStatus = "Healthy";
            StatusBrush = "SuccessBrush";
            StatusMessage = $"Scan completed — {devices.Count} devices found.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
            App.Logger.Error("Dashboard scan failed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Checking for updates...";

            var devices = await App.Scanner.ScanAsync();
            var recommendations = await App.Resolver.ResolveAsync(devices);
            UpdateCount = recommendations.Count;
            
            if (UpdateCount > 0)
            {
                SystemHealthStatus = "Updates Available";
                StatusBrush = "WarningBrush";
            }
            else
            {
                SystemHealthStatus = "Healthy";
                StatusBrush = "SuccessBrush";
            }

            StatusMessage = $"Found {recommendations.Count} updates.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Update check failed: {ex.Message}";
            App.Logger.Error("Dashboard update check failed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
