using Microsoft.UI.Xaml.Controls;
using OpenDriverUpdater.App.ViewModels;

namespace OpenDriverUpdater.App.Views;

public sealed partial class DevicesPage : Page
{
    public DevicesPage()
    {
        this.InitializeComponent();
        // Shared view model: results persist when navigating away and back.
        DataContext = DevicesViewModel.Shared;
    }
}
