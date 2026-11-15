using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using OpenDriverUpdater.App.Views;

namespace OpenDriverUpdater.App;

public sealed partial class MainWindow : Window
{
    private readonly UiSettings _uiSettings = UiSettings.Load();

    // Pane-resize drag state
    private bool _isResizing;
    private double _dragStartX;
    private double _dragStartLength;

    public MainWindow()
    {
        this.InitializeComponent();
        Title = $"{AppInfo.DisplayName} {AppInfo.VersionLabel}";

        // Set Window Icon
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "odu_favicon.ico"));

        // Restore the saved navigation pane width
        SetPaneLength(_uiSettings.NavPaneLength);
        UpdateGripVisibility();

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
                "Software" => typeof(SoftwarePage),
                "Logs" => typeof(LogsPage),
                _ => typeof(DevicesPage)
            };

            ContentFrame.Navigate(pageType);
        }
    }

    // ── Resizable navigation pane ───────────────────────────────────────
    // The hamburger button still shows/hides the labels (IsPaneOpen);
    // the grip is only shown while the pane is open.

    private void NavView_PaneOpened(NavigationView sender, object args) => UpdateGripVisibility();
    private void NavView_PaneClosed(NavigationView sender, object args) => UpdateGripVisibility();

    private void UpdateGripVisibility()
    {
        PaneGrip.Visibility = NavView.IsPaneOpen ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SetPaneLength(double length)
    {
        length = UiSettings.ClampPaneLength(length);
        NavView.OpenPaneLength = length;
        // Centre the 8px grip on the pane's right edge
        PaneGrip.Margin = new Thickness(length - PaneGrip.Width / 2, 0, 0, 0);
    }

    private void PaneGrip_PointerEntered(object sender, PointerRoutedEventArgs e) => PaneGripLine.Opacity = 0.8;

    private void PaneGrip_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!_isResizing)
            PaneGripLine.Opacity = 0;
    }

    private void PaneGrip_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isResizing = PaneGrip.CapturePointer(e.Pointer);
        _dragStartX = e.GetCurrentPoint(RootGrid).Position.X;
        _dragStartLength = NavView.OpenPaneLength;
        e.Handled = true;
    }

    private void PaneGrip_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isResizing) return;
        var x = e.GetCurrentPoint(RootGrid).Position.X;
        SetPaneLength(_dragStartLength + (x - _dragStartX));
        e.Handled = true;
    }

    private void PaneGrip_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        PaneGrip.ReleasePointerCapture(e.Pointer);
        EndResize();
        e.Handled = true;
    }

    private void PaneGrip_PointerCaptureLost(object sender, PointerRoutedEventArgs e) => EndResize();

    // Double-click the grip to reset to the default width
    private void PaneGrip_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        SetPaneLength(UiSettings.DefaultPaneLength);
        SavePaneLength();
    }

    private void EndResize()
    {
        if (!_isResizing) return;
        _isResizing = false;
        PaneGripLine.Opacity = 0;
        SavePaneLength();
    }

    private void SavePaneLength()
    {
        if (Math.Abs(_uiSettings.NavPaneLength - NavView.OpenPaneLength) < 0.5) return;
        _uiSettings.NavPaneLength = NavView.OpenPaneLength;
        _uiSettings.Save();
    }
}
