using Microsoft.UI.Xaml.Controls;

namespace OpenDriverUpdater.App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
        VersionText.Text = $"{AppInfo.DisplayName} {AppInfo.VersionLabel}";
    }
}
