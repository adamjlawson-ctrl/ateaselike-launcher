namespace AtEase.App.Services;

public sealed class ActionResult
{
    public bool IsSuccess { get; }

    public string Message { get; }

    private ActionResult(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    public static ActionResult Success(string message)
    {
        return new ActionResult(true, message);
    }

    public static ActionResult Failure(string message)
    {
        return new ActionResult(false, message);
    }
}