using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace AtEase.App.Helpers;

public sealed class IconImageSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        var path = value as string;
        if (!TryGetIconUri(path, out var uri))
        {
            return null;
        }

        try
        {
            return new BitmapImage(uri);
        }
        catch
        {
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }

    private static bool TryGetIconUri(string? iconHint, out Uri uri)
    {
        uri = null!;

        var path = (iconHint ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        if (File.Exists(path) && IsSupportedImageExtension(path))
        {
            uri = new Uri(path, UriKind.Absolute);
            return true;
        }

        if (Uri.TryCreate(path, UriKind.Absolute, out var parsed) &&
            (parsed.Scheme.Equals("ms-appx", StringComparison.OrdinalIgnoreCase) ||
             parsed.Scheme.Equals("ms-appdata", StringComparison.OrdinalIgnoreCase) ||
             parsed.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase)))
        {
            uri = parsed;
            return true;
        }

        return false;
    }

    private static bool IsSupportedImageExtension(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ico", StringComparison.OrdinalIgnoreCase);
    }
}
