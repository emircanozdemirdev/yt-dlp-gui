namespace YtDlpGui.App.Models;

public static class DownloadProfilesCatalog
{
    public static IReadOnlyList<DownloadProfile> Defaults =>
    [
        new()
        {
            Id = "best",
            DisplayName = "Best",
            FormatSelector = "best"
        },
        new()
        {
            Id = "1080p",
            DisplayName = "1080p",
            FormatSelector = "bestvideo[height<=1080]+bestaudio/best[height<=1080]"
        },
        new()
        {
            Id = "audio-only",
            DisplayName = "Audio Only",
            FormatSelector = "bestaudio"
        },
        new()
        {
            Id = "archive-mode",
            DisplayName = "Archive Mode",
            FormatSelector = "best",
            UseDownloadArchive = true
        }
    ];
}
