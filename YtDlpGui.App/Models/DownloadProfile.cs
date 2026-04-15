namespace YtDlpGui.App.Models;

public sealed class DownloadProfile
{
    public string Id { get; set; } = "best";
    public string DisplayName { get; set; } = "Best";
    public string FormatSelector { get; set; } = "best";
    public bool UseDownloadArchive { get; set; }
}
