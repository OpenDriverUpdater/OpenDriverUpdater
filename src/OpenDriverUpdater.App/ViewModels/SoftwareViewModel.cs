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
    [NotifyCanExecuteChangedFor(nameof(UpdateAllCommand))]
    private bool _isLoading;

    /// <summary>"Update All" is updating apps one after another.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    [NotifyCanExecuteChangedFor(nameof(CheckForUpdatesCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpdateAllCommand))]
    [NotifyPropertyChangedFor(nameof(ShowUpdateAll))]
    private bool _isUpdatingAll;

    private bool _stopRequested;

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
    private bool CanRun() => !IsLoading && !IsUpdatingAll && !Apps.Any(a => a.IsBusy);

    /// <summary>Apps with an update winget can install, not yet done and not running.</summary>
    private List<SoftwareItemViewModel> PendingUpdates() =>
        Apps.Where(a => a.NeedsUpdate && a.App.IsTracked && !a.IsBusy).ToList();

    public int PendingUpdateCount => PendingUpdates().Count;

    /// <summary>Shown after "Check for Updates" finds at least one update.</summary>
    public bool ShowUpdateAll => !IsUpdatingAll && PendingUpdateCount > 0;

    public string UpdateAllText => $"Update All ({PendingUpdateCount})";

    private void OnItemBusyChanged(object? sender, EventArgs e)
    {
        RefreshCommand.NotifyCanExecuteChanged();
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
        NotifyUpdateAllState();
    }

    private void NotifyUpdateAllState()
    {
        OnPropertyChanged(nameof(PendingUpdateCount));
        OnPropertyChanged(nameof(ShowUpdateAll));
        OnPropertyChanged(nameof(UpdateAllText));
        UpdateAllCommand.NotifyCanExecuteChanged();
    }

    private bool CanUpdateAll() => !IsLoading && !IsUpdatingAll && PendingUpdateCount > 0;

    /// <summary>
    /// Updates every app that needs it, one at a time. A failure doesn't stop
    /// the batch; Stop finishes the current update and skips the rest.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanUpdateAll))]
    private async Task UpdateAllAsync()
    {
        // winget is part of App Installer: updating it mid-batch can break the
        // remaining updates, so it always goes last.
        var queue = PendingUpdates()
            .OrderBy(a => a.App.Id.StartsWith("Microsoft.AppInstaller", StringComparison.OrdinalIgnoreCase)
                          || a.App.Id.StartsWith("Microsoft.DesktopAppInstaller", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (queue.Count == 0) return;

        _stopRequested = false;
        IsUpdatingAll = true;
        foreach (var item in queue)
            item.IsLocked = true;

        int updated = 0, failed = 0, processed = 0;
        try
        {
            App.DiscordPresence.SetPresence($"Updating {queue.Count} apps", "Update All");

            foreach (var item in queue)
            {
                if (_stopRequested) break;

                processed++;
                // Skip anything that changed since the batch started.
                if (item.IsCompleted || item.IsBusy) continue;

                StatusMessage = $"Updating {processed} of {queue.Count}: {item.App.Name}...";

                await item.UpdateCommand.ExecuteAsync(null);

                if (item.IsCompleted) updated++;
                else failed++;

                item.IsLocked = false;
            }
        }
        catch (Exception ex)
        {
            App.Logger.Error("Update All (software) failed", ex);
        }
        finally
        {
            foreach (var item in queue)
                item.IsLocked = false;
            IsUpdatingAll = false;
            NotifyUpdateAllState();
        }

        var skipped = queue.Count - processed;
        var summary = $"Updated {updated} of {queue.Count} app{(queue.Count == 1 ? "" : "s")}.";
        if (failed > 0) summary += $" {failed} failed (see the tile for details).";
        if (skipped > 0) summary += $" {skipped} skipped because you pressed Stop.";
        StatusMessage = summary;
        App.DiscordPresence.SetPresence($"{updated} apps updated", "Ready");
    }

    [RelayCommand]
    private void StopUpdateAll()
    {
        if (!IsUpdatingAll) return;
        _stopRequested = true;
        StatusMessage = "Stopping after the current update finishes...";
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
            NotifyUpdateAllState();
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
            NotifyUpdateAllState();
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
