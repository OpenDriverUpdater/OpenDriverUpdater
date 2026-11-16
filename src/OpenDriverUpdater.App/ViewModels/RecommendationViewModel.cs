using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.App.ViewModels;

public partial class RecommendationViewModel : ObservableObject
{
    private readonly UpdateRecommendation _recommendation;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInstall))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInstall))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private bool _isCompleted;

    /// <summary>
    /// Set while "Update All" is running, so this tile's own Install button
    /// can't start a second install alongside the batch.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInstall))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private bool _isLocked;

    /// <summary>The Install button is clickable only when idle, not yet installed and not locked.</summary>
    public bool CanInstall => !IsBusy && !IsCompleted && !IsLocked;

    [ObservableProperty]
    private bool _hasError;

    public UpdateRecommendation Recommendation => _recommendation;
    public string VersionInfo => $"{_recommendation.InstalledVersion} -> {_recommendation.CandidateVersion}";

    public RecommendationViewModel(UpdateRecommendation recommendation)
    {
        _recommendation = recommendation;
    }

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallAsync()
    {
        if (IsBusy || IsCompleted || _recommendation.Candidate is not { } candidate) return;

        try
        {
            IsBusy = true;
            HasError = false;

            // 0. İndirme URL'sini çözümle — birincil aday başarısız olursa
            //    yedek adayları dene (Catalog'da indirilemeyen kopyalar var).
            if (string.IsNullOrEmpty(candidate.DownloadUrl))
            {
                Status = "Resolving download...";
                foreach (var attempt in new[] { candidate }.Concat(_recommendation.Alternates))
                {
                    if (string.IsNullOrEmpty(attempt.SourceReference))
                        continue;

                    var source = App.DriverSources.FirstOrDefault(s => s.SourceType == attempt.SourceType);
                    if (source is null)
                        continue;

                    var url = await source.ResolveDownloadUrlAsync(attempt.SourceReference);
                    if (!string.IsNullOrEmpty(url))
                    {
                        attempt.DownloadUrl = url;
                        candidate = attempt;
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(candidate.DownloadUrl))
            {
                Status = "Failed: No downloadable file for this driver";
                HasError = true;
                return;
            }

            // 1. Download
            Status = "Downloading...";
            var downloadDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpenDriverUpdater", "downloads");

            var progressHandler = new Progress<double>(p => Progress = p * 100);
            var package = await App.DownloadManager.DownloadAsync(candidate, downloadDir, progressHandler);

            // System safety: create restore point
            Status = "Preparing system safety...";
            var safety = new OpenDriverUpdater.Infrastructure.Safety.SystemSafetyService(App.Logger);
            var restoreOk = await safety.CreateRestorePointAsync(
                $"OpenDriverUpdater driver update {candidate.Version}", default);
            if (!restoreOk)
                App.Logger.Warn("Continuing without a new restore point (Windows allows one per 24h by default)");

            // 2. Install
            Status = "Installing...";
            Progress = 0; // Reset for install phase
            var installProgress = new Progress<double>(p => Progress = p * 100);
            var result = await App.Installer.InstallAsync(package, installProgress);

            if (result.Success)
            {
                Status = result.RequiresRestart ? "Restart Required" : "Completed";
                IsCompleted = true;
            }
            else
            {
                Status = $"Error: {result.Message}";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            Status = $"Failed: {ex.Message}";
            HasError = true;
            App.Logger.Error("Update orchestration failed", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
