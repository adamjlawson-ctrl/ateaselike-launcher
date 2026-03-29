using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace AtEase.App.Helpers;

public sealed class IconImageVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var iconHint = (value as string ?? string.Empty).Trim();
        var hasImage = HasImage(iconHint);
        var invert = string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase);

        if (invert)
        {
            hasImage = !hasImage;
        }

        return hasImage ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }

    private static bool HasImage(string iconHint)
    {
        if (string.IsNullOrWhiteSpace(iconHint))
        {
            return false;
        }

        if (File.Exists(iconHint))
        {
            var extension = Path.GetExtension(iconHint);
            return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".ico", StringComparison.OrdinalIgnoreCase);
        }

        return Uri.TryCreate(iconHint, UriKind.Absolute, out var uri)
            && (uri.Scheme.Equals("ms-appx", StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals("ms-appdata", StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase));
    }
}
