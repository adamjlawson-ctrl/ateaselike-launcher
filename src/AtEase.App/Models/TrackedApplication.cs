namespace AtEase.App.Models;

public class TrackedApplication
{
    public required string DisplayName { get; init; }

    public required string Path { get; init; }

    public required int ProcessId { get; init; }

    public string MenuLabel => $"{DisplayName} ({ProcessId})";
}
