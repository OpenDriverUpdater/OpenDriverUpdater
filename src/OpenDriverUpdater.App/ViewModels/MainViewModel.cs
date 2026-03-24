using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenDriverUpdater.App.ViewModels;

/// <summary>
/// Ana navigasyon ViewModel'i.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "OpenDriverUpdater";

    [ObservableProperty]
    private bool _isLoading;
}
