using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenDriverUpdater.App.Views;

namespace OpenDriverUpdater.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        Title = $"{AppInfo.DisplayName} {AppInfo.VersionLabel}";

        // Set Window Icon
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "odu_favicon.ico"));

        // Default page — selecting the item also navigates via SelectionChanged
        NavView.SelectedItem = DevicesNavItem;
        if (ContentFrame.Content is null)
            ContentFrame.Navigate(typeof(DevicesPage));
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
                "Devices" => typeof(DevicesPage),
                "Logs" => typeof(LogsPage),
                _ => typeof(DevicesPage)
            };

            ContentFrame.Navigate(pageType);
        }
    }
}
