using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace OpenDriverUpdater.App
{
    public class MainWindow : Window
    {
        private TextBlock _statusText = null!;
        private ProgressBar _scanProgress = null!;

        public MainWindow()
        {
            Title = "OpenDriverUpdater v2 - Avant-Garde";
            
            // Replicating the "OduNeon" glassmorphic deep dark aesthetic
            var rootGrid = new Grid
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 10, 10, 15))
            };

            var mainPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 24
            };

            var title = new TextBlock
            {
                Text = "OPEN DRIVER UPDATER",
                FontSize = 32,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                CharacterSpacing = 300,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var subtitle = new TextBlock
            {
                Text = "C# METAMORPHOSIS • XAML OVERRIDDEN",
                FontSize = 13,
                Foreground = new SolidColorBrush(Colors.DodgerBlue),
                CharacterSpacing = 150,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _statusText = new TextBlock
            {
                Text = "System stabilized. Awaiting directives.",
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.LightGray),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 40, 0, 0)
            };

            _scanProgress = new ProgressBar
            {
                Width = 400,
                Height = 4,
                IsIndeterminate = false,
                Visibility = Visibility.Collapsed,
                Foreground = new SolidColorBrush(Colors.DodgerBlue),
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 30, 30, 40)),
                Margin = new Thickness(0, 20, 0, 0)
            };

            var scanBtn = new Button
            {
                Content = "INITIATE DIAGNOSTIC",
                Width = 260,
                Height = 48,
                CornerRadius = new CornerRadius(0),
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 20, 20, 28)),
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 60, 60, 80)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                Style = null
            };

            scanBtn.Click += async (s, e) => 
            {
                scanBtn.IsEnabled = false;
                _statusText.Text = "Interrogating WMI endpoints...";
                _scanProgress.Visibility = Visibility.Visible;
                _scanProgress.IsIndeterminate = true;

                try 
                {
                    // Trigger the injected hardware scanner
                    var devices = await App.Scanner.ScanAsync();
                    _statusText.Text = $"Diagnostic complete: Detected {devices.Count} hardware nodes.";
                }
                catch (System.Exception ex)
                {
                    _statusText.Text = "Hardware diagnostic encountered an anomaly.";
                    App.Logger.Error("Scan UI failed", ex);
                }
                finally 
                {
                    _scanProgress.IsIndeterminate = false;
                    _scanProgress.Visibility = Visibility.Collapsed;
                    scanBtn.IsEnabled = true;
                }
            };

            mainPanel.Children.Add(title);
            mainPanel.Children.Add(subtitle);
            mainPanel.Children.Add(_statusText);
            mainPanel.Children.Add(_scanProgress);
            mainPanel.Children.Add(scanBtn);

            rootGrid.Children.Add(mainPanel);
            Content = rootGrid;
        }
    }
}
