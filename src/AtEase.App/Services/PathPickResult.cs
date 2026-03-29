namespace AtEase.App.Services;

public sealed class PathPickResult
{
    public bool IsSuccess { get; }

    public bool IsCancelled { get; }

    public string Path { get; }

    public string Message { get; }

    private PathPickResult(bool isSuccess, bool isCancelled, string path, string message)
    {
        IsSuccess = isSuccess;
        IsCancelled = isCancelled;
        Path = path;
        Message = message;
    }

    public static PathPickResult Success(string path)
    {
        return new PathPickResult(true, false, path, string.Empty);
    }

    public static PathPickResult Cancelled()
    {
        return new PathPickResult(false, true, string.Empty, "Path selection was cancelled.");
    }

    public static PathPickResult Failure(string message)
    {
        return new PathPickResult(false, false, string.Empty, message);
    }
}