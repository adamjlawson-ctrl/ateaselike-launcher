using Microsoft.UI.Xaml.Data;

namespace AtEase.App.Helpers;

public sealed class PathExistenceHintConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var path = (value as string ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            return "No path set";
        }

        var mode = (parameter as string ?? string.Empty).Trim().ToLowerInvariant();
        return mode switch
        {
            "file" => File.Exists(path) ? "Valid path" : "File not found",
            "folder" => Directory.Exists(path) ? "Valid path" : "Folder not found",
            _ => "No path set"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}
