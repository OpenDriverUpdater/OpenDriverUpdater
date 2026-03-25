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
    private string _status = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private bool _hasError;

    public UpdateRecommendation Recommendation => _recommendation;
    public string DeviceName => _recommendation.DeviceId; // TODO: Get full name
    public string VersionInfo => $"{_recommendation.InstalledVersion} -> {_recommendation.CandidateVersion}";

    public RecommendationViewModel(UpdateRecommendation recommendation)
    {
        _recommendation = recommendation;
    }

    [RelayCommand]
    private async Task InstallAsync()
    {
        if (IsBusy || IsCompleted || _recommendation.Candidate == null) return;

        try
        {
            IsBusy = true;
            HasError = false;
            
            // 1. Download
            Status = "Downloading...";
            var downloadDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpenDriverUpdater", "downloads");

            var progressHandler = new Progress<double>(p => Progress = p * 100);
            var package = await App.DownloadManager.DownloadAsync(_recommendation.Candidate, downloadDir, progressHandler);

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
