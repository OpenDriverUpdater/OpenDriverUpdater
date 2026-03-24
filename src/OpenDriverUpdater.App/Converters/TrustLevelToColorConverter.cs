using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using OpenDriverUpdater.Core.Enums;

namespace OpenDriverUpdater.App.Converters;

/// <summary>
/// TrustLevel → SolidColorBrush dönüştürücü.
/// </summary>
public sealed class TrustLevelToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TrustLevel level)
        {
            return level switch
            {
                TrustLevel.Verified => new SolidColorBrush(Colors.LimeGreen),
                TrustLevel.Trusted => new SolidColorBrush(Colors.DodgerBlue),
                TrustLevel.Unknown => new SolidColorBrush(Colors.Orange),
                TrustLevel.Untrusted => new SolidColorBrush(Colors.OrangeRed),
                TrustLevel.Blacklisted => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Gray),
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}
