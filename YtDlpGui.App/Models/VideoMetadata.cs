namespace YtDlpGui.App.Models;

public sealed class VideoMetadata
{
    public string Url { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Uploader { get; init; } = string.Empty;
    public TimeSpan? Duration { get; init; }
    public IReadOnlyList<FormatOption> Formats { get; init; } = [];
}
