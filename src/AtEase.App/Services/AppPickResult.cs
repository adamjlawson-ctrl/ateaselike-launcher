using AtEase.App.Models;

namespace AtEase.App.Services;

public sealed class AppPickResult
{
    public bool IsSuccess { get; }

    public bool IsCancelled { get; }

    public AppPickerEntry? Entry { get; }

    public string Message { get; }

    private AppPickResult(bool isSuccess, bool isCancelled, AppPickerEntry? entry, string message)
    {
        IsSuccess = isSuccess;
        IsCancelled = isCancelled;
        Entry = entry;
        Message = message;
    }

    public static AppPickResult Success(AppPickerEntry entry)
    {
        return new AppPickResult(true, false, entry, string.Empty);
    }

    public static AppPickResult Cancelled()
    {
        return new AppPickResult(false, true, null, "App selection was cancelled.");
    }

    public static AppPickResult Failure(string message)
    {
        return new AppPickResult(false, false, null, message);
    }
}