namespace AtEase.App.Models;

public sealed class AppPickerEntry
{
    public required string DisplayName { get; init; }

    public required string LaunchPath { get; init; }

    public string ResolvedTargetPath { get; init; } = string.Empty;

    public string IconHint { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;
}