using AtEase.App.Services.Interfaces;

namespace AtEase.App.Services.Implementations;

public class FileSystemService : IFileSystemService
{
    public IReadOnlyList<string> GetDirectories(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
        {
            return [];
        }

        return Directory.GetDirectories(rootPath)
            .OrderBy(path => path)
            .ToList();
    }
}
