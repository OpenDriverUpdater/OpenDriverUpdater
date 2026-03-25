using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenDriverUpdater.Core.Models;

namespace OpenDriverUpdater.App.ViewModels;

public partial class UpdatesViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<RecommendationViewModel> Recommendations { get; } = [];

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
                Recommendations.Add(new RecommendationViewModel(r));
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

}
