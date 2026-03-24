using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenDriverUpdater.App.Views;

namespace OpenDriverUpdater.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "OpenDriverUpdater";

        // Varsayılan sayfa
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            var pageType = tag switch
            {
                "Dashboard" => typeof(DashboardPage),
                "Devices" => typeof(DevicesPage),
                "Updates" => typeof(UpdatesPage),
                "Logs" => typeof(LogsPage),
                _ => typeof(DashboardPage)
            };

            ContentFrame.Navigate(pageType);
        }
    }
}
