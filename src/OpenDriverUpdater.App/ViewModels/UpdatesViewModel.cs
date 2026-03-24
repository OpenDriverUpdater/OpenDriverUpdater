using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.App.ViewModels;

public partial class UpdatesViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<UpdateRecommendation> Recommendations { get; } = [];

    [RelayCommand]
    private async Task CheckAsync()
    {
        try
        {
            IsLoading = true;
            Recommendations.Clear();

            var devices = await App.Scanner.ScanAsync();
            var results = await App.Resolver.ResolveAsync(devices);

            foreach (var r in results)
                Recommendations.Add(r);
        }
        catch (Exception ex)
        {
            App.Logger.Error("Update check failed", ex);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DownloadAllAsync()
    {
        var downloadDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenDriverUpdater", "downloads");

        foreach (var rec in Recommendations)
        {
            if (rec.Candidate is null) continue;

            try
            {
                await App.DownloadManager.DownloadAsync(rec.Candidate, downloadDir);
                App.Logger.Info("Downloaded: {Title}", rec.Candidate.Title);
            }
            catch (Exception ex)
            {
                App.Logger.Error("Download failed for {Title}", ex, rec.Candidate.Title);
            }
        }
    }
}
