using Microsoft.UI.Xaml.Controls;
using OpenDriverUpdater.App.ViewModels;

namespace OpenDriverUpdater.App.Views;

public sealed partial class SoftwarePage : Page
{
    public SoftwarePage()
    {
        this.InitializeComponent();
        // Shared view model: results persist when navigating away and back.
        DataContext = SoftwareViewModel.Shared;
    }
}
