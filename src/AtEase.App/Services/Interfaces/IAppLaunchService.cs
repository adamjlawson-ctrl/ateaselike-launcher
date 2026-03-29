namespace AtEase.App.Services.Interfaces;

public interface IAppLaunchService
{
    bool TryLaunch(string path, string? arguments = null);
}
