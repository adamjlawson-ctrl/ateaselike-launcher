using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace AtEase.App.Helpers;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isTrue = value is bool b && b;
        var invert = string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase);

        if (invert)
        {
            isTrue = !isTrue;
        }

        return isTrue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
