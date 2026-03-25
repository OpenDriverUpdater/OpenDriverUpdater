using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace OpenDriverUpdater.App.Converters;

public class ResourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string resourceKey && Application.Current.Resources.TryGetValue(resourceKey, out var resource))
        {
            return resource;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
