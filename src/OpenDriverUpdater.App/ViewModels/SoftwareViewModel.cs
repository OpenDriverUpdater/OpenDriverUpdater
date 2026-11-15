using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.App.ViewModels;

public partial class SoftwareViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    private bool _isLoading;

    [ObservableProperty]
    private string? _statusMessage;

    public ObservableCollection<SoftwareItemViewModel> Apps { get; } = [];

    /// <summary>
    /// One instance for the app's lifetime, so results survive navigating away.
    /// They are only replaced by Refresh / Check for Updates, or closing the app.
    /// </summary>
    public static SoftwareViewModel Shared => _shared ??= new SoftwareViewModel();
    private static SoftwareViewModel? _shared;

    private SoftwareViewModel()
    {
        _ = RefreshAsync();
    }

    // Replacing the tiles mid-update would orphan the running update.
    private bool CanRun() => !IsLoading && !Apps.Any(a => a.IsBusy);

    private void OnItemBusyChanged(object? sender, EventArgs e)
    {
        RefreshCommand.NotifyCanExecuteChanged();
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Lists installed apps (no update status shown, like the Devices page).</summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RefreshAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Finding installed apps...";
            var apps = await App.SoftwareUpdates.GetInstalledAppsAsync(refreshSources: false);
            Fill(apps, isChecked: false);
            StatusMessage = $"{Apps.Count} installed apps found. Click \"Check for Updates\" to see which need updating.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Couldn't list installed apps: {ex.Message}";
            App.Logger.Error("Software refresh failed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Refreshes winget's package sources, then shows each app's update status.</summary>
    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task CheckForUpdatesAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Checking for updates... this can take a minute.";
            App.DiscordPresence.SetPresence("Checking for app updates...", "winget");

            var apps = await App.SoftwareUpdates.GetInstalledAppsAsync(refreshSources: true);
            Fill(apps, isChecked: true);

            var updates = Apps.Count(a => a.NeedsUpdate);
            var untracked = Apps.Count(a => !a.App.IsTracked);
            var summary = updates == 0
                ? "All tracked apps are up to date."
                : $"{updates} app{(updates == 1 ? "" : "s")} need{(updates == 1 ? "s" : "")} updating.";
            if (untracked > 0)
                summary += $" {untracked} app{(untracked == 1 ? " isn't" : "s aren't")} tracked by winget, so {(untracked == 1 ? "its" : "their")} update status is unknown.";
            StatusMessage = summary;

            App.DiscordPresence.SetPresence($"{updates} app updates found", "Ready");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Update check failed: {ex.Message}";
            App.Logger.Error("Software update check failed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Fill(IReadOnlyList<InstalledApp> apps, bool isChecked)
    {
        foreach (var old in Apps)
            old.BusyChanged -= OnItemBusyChanged;
        Apps.Clear();

        var ordered = apps
            .OrderByDescending(a => isChecked && a.HasUpdate)   // needs updating first
            .ThenByDescending(a => isChecked && a.IsTracked)    // then up to date, then not tracked
            .ThenBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase);

        foreach (var app in ordered)
        {
            var item = new SoftwareItemViewModel(app) { IsChecked = isChecked };
            item.BusyChanged += OnItemBusyChanged;
            Apps.Add(item);
        }
    }
}
