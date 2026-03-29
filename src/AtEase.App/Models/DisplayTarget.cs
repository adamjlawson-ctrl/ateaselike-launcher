namespace AtEase.App.Models;

public class DisplayTarget
{
    public string Id { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }

    public int Left { get; set; }

    public int Top { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public string PickerLabel => IsPrimary
        ? $"{Label} (Primary) - {Width} x {Height}"
        : $"{Label} - {Width} x {Height}";
}
