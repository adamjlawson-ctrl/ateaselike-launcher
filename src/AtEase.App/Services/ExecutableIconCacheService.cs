using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;

namespace AtEase.App.Services;

public sealed class ExecutableIconCacheService
{
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _cacheFolderPath;

    public ExecutableIconCacheService()
    {
        _cacheFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AtEaseWin11",
            "icon-cache");
    }

    public string? TryGetExecutableIconImagePath(string appPath)
    {
        if (string.IsNullOrWhiteSpace(appPath))
        {
            return null;
        }

        if (!File.Exists(appPath))
        {
            return null;
        }

        var extension = Path.GetExtension(appPath);
        if (!extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var key = BuildCacheKey(appPath);
            if (_cache.TryGetValue(key, out var cachedPath) && File.Exists(cachedPath))
            {
                return cachedPath;
            }

            Directory.CreateDirectory(_cacheFolderPath);

            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key))).ToLowerInvariant();
            var imagePath = Path.Combine(_cacheFolderPath, $"{hash}.png");

            if (!File.Exists(imagePath))
            {
                using var icon = Icon.ExtractAssociatedIcon(appPath);
                if (icon is null)
                {
                    return null;
                }

                using var bitmap = icon.ToBitmap();
                bitmap.Save(imagePath, ImageFormat.Png);
            }

            _cache[key] = imagePath;
            return imagePath;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildCacheKey(string appPath)
    {
        var normalizedPath = appPath.Trim();
        var stamp = File.GetLastWriteTimeUtc(normalizedPath).Ticks;
        return $"{normalizedPath}|{stamp}";
    }
}
