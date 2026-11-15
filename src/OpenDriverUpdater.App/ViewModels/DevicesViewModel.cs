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
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    public ObservableCollection<DeviceItemViewModel> Devices { get; } = [];

    /// <summary>
    /// One instance for the app's lifetime, so scan and update-check results
    /// survive navigating to other pages. They are only replaced when Refresh
    /// or Check for Updates is pressed, or the app is closed.
    /// </summary>
    public static DevicesViewModel Shared => _shared ??= new DevicesViewModel();
    private static DevicesViewModel? _shared;

    private DevicesViewModel()
    {
        _ = RefreshAsync();
    }

    // Replacing the tiles mid-install would orphan the running install,
    // so both buttons are disabled while any install is in progress.
    private bool CanRun() => !IsLoading && !Devices.Any(d => d.Update?.IsBusy == true);

    private void OnItemBusyChanged(object? sender, EventArgs e)
    {
        RefreshCommand.NotifyCanExecuteChanged();
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Scanning devices...";
            await ScanIntoListAsync();
            StatusMessage = $"{Devices.Count} devices found. Click \"Check for Updates\" to look for newer drivers.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Device scan failed: {ex.Message}";
            App.Logger.Error("Device refresh failed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task CheckForUpdatesAsync()
    {
        try
        {
            IsLoading = true;

            if (Devices.Count == 0)
            {
                StatusMessage = "Scanning devices...";
                await ScanIntoListAsync();
            }

            StatusMessage = "Checking for updates... this can take a few minutes.";
            App.DiscordPresence.SetPresence("Checking for updates...", "Microsoft Update Catalog");

            var devices = Devices.Select(d => d.Device).ToList();
            var results = await App.Resolver.ResolveAsync(devices);
            // Never offer a candidate whose hardware IDs don't match the device
            // (TrustLevel.Untrusted) — see docs/HANDOVER.md "Trust rules".
            var byDevice = results
                .Where(r => r.TrustScore is null || r.TrustScore.Level > TrustLevel.Untrusted)
                .GroupBy(r => r.DeviceId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

            foreach (var item in Devices)
            {
                item.Update = byDevice.TryGetValue(item.Device.Id, out var rec)
                    ? new RecommendationViewModel(rec)
                    : null;
                item.IsChecked = true;
            }

            // Devices that need updating go to the top.
            var reordered = Devices
                .OrderByDescending(d => d.NeedsUpdate)
                .ThenByDescending(d => GetPriority(d.Device))
                .ThenBy(d => d.Device.Class)
                .ThenBy(d => d.Device.Name)
                .ToList();
            Devices.Clear();
            foreach (var item in reordered)
                Devices.Add(item);

            var count = byDevice.Count;
            StatusMessage = count == 0
                ? "All devices are up to date."
                : $"{count} device{(count == 1 ? "" : "s")} need{(count == 1 ? "s" : "")} updating.";
            App.DiscordPresence.SetPresence($"{count} updates found", "Ready to install");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Update check failed: {ex.Message}";
            App.Logger.Error("Update check failed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ScanIntoListAsync()
    {
        foreach (var old in Devices)
            old.BusyChanged -= OnItemBusyChanged;
        Devices.Clear();
        App.DiscordPresence.SetPresence("Scanning hardware...", "OpenDriverUpdater");

        var devices = await App.Scanner.ScanAsync();
        var ordered = devices.OrderByDescending(GetPriority).ThenBy(d => d.Class).ThenBy(d => d.Name).ToList();
        foreach (var device in ordered)
        {
            var item = new DeviceItemViewModel(device);
            item.BusyChanged += OnItemBusyChanged;
            Devices.Add(item);
        }

        App.DiscordPresence.SetPresence($"{devices.Count} devices found", "Ready");
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
