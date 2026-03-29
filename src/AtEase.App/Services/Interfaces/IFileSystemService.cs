namespace AtEase.App.Services.Interfaces;

public interface IFileSystemService
{
    IReadOnlyList<string> GetDirectories(string rootPath);
}
