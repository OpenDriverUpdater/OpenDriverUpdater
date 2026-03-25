using Microsoft.UI.Xaml.Data;

namespace OpenDriverUpdater.App.Converters;

public class DeviceClassToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var deviceClass = value?.ToString()?.ToLowerInvariant() ?? "";

        return deviceClass switch
        {
            "display" => "\uEC70",     // Monitor
            "net" => "\uE774",         // Network
            "audioendpoint" => "\uE7F6", // Speaker
            "media" => "\uE7F6",       // Media/Audio
            "mouse" => "\uE962",       // Mouse
            "keyboard" => "\uE92E",    // Keyboard
            "processor" => "\uE950",   // CPU
            "bluetooth" => "\uE702",   // Bluetooth
            "usb" => "\uE88E",         // USB
            "diskdrive" => "\uEDA2",   // Disk
            "camera" => "\uE722",      // Camera
            "image" => "\uE722",       // Image/Camera
            "battery" => "\uEBB3",     // Battery
            "biometric" => "\uE928",   // Fingerprint
            _ => "\uE95A"              // Generic Chip
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
