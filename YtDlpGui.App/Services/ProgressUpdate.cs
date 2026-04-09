namespace YtDlpGui.App.Services;

public sealed class ProgressUpdate
{
    public double? Percent { get; init; }
    public string? Speed { get; init; }
    public string? Eta { get; init; }
}
