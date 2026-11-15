using Microsoft.UI.Xaml.Controls;
using OpenDriverUpdater.App.ViewModels;

namespace OpenDriverUpdater.App.Views;

public sealed partial class RegistryPage : Page
{
    public RegistryPage()
    {
        this.InitializeComponent();
        // Shared view model: the report persists when navigating away and back.
        DataContext = RegistryViewModel.Shared;
    }
}
