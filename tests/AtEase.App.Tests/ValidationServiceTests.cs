using AtEase.App.Models;
using AtEase.App.Services.Implementations;

namespace AtEase.App.Tests;

public class ValidationServiceTests
{
    [Fact]
    public void ValidateSettings_WithDuplicateIds_ReturnsError()
    {
        var service = new ValidationService();
        var id = "dup";

        var settings = new AppSettings
        {
            Apps =
            [
                new AppItem { Id = id, DisplayName = "Calc", Path = "calc.exe" }
            ],
            Folders =
            [
                new FolderItem { Id = id, DisplayName = "Desktop", Path = "C:\\Users\\Test\\Desktop" }
            ]
        };

        var errors = service.ValidateSettings(settings);

        Assert.Contains(errors, e => e.Contains("duplicate IDs", StringComparison.OrdinalIgnoreCase));
    }
}
