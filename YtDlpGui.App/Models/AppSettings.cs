namespace YtDlpGui.App.Models;

public sealed class AppSettings
{
    public string YtDlpPath { get; set; } = "yt-dlp";
    public string FfmpegPath { get; set; } = "ffmpeg";
    public string OutputDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Downloads");
    public string FileNameTemplate { get; set; } = "%(title)s.%(ext)s";
    public int MaxParallelDownloads { get; set; } = 2;
    public int Retries { get; set; } = 3;
    public string? Proxy { get; set; }
    public int? RateLimitKbps { get; set; }
}
