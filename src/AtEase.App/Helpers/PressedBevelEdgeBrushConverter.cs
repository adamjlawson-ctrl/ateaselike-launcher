using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace AtEase.App.Helpers;

public sealed class PressedBevelEdgeBrushConverter : IValueConverter
{
    private static readonly SolidColorBrush WhiteEdgeBrush = new() { Color = Windows.UI.Color.FromArgb(255, 255, 255, 255) };
    private static readonly SolidColorBrush GrayEdgeBrush = new() { Color = Windows.UI.Color.FromArgb(255, 119, 119, 119) };

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var isPressed = value is bool pressed && pressed;
        var edge = (parameter as string ?? string.Empty).Trim().ToLowerInvariant();

        return edge switch
        {
            "tl" => isPressed ? GrayEdgeBrush : WhiteEdgeBrush,
            "br" => isPressed ? WhiteEdgeBrush : GrayEdgeBrush,
            _ => isPressed ? GrayEdgeBrush : WhiteEdgeBrush
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
