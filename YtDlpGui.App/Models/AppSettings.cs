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
    public bool BandwidthScheduleEnabled { get; set; }
    public int? BandwidthLimitKbps { get; set; }
    public string BandwidthWindowStart { get; set; } = "23:00";
    public string BandwidthWindowEnd { get; set; } = "07:00";
    public DuplicatePolicy DuplicatePolicy { get; set; } = DuplicatePolicy.Allow;
    public bool NotifyOnCompletion { get; set; }
    public bool PlaySoundOnCompletion { get; set; }
    public bool EnablePlaylistDownload { get; set; } = true;
    public string DownloadArchivePath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YtDlpGui", "download-archive.txt");
    public List<DownloadProfile> DownloadProfiles { get; set; } = [.. DownloadProfilesCatalog.Defaults];
    public string SelectedProfileId { get; set; } = "best";
    public bool DeleteTempFilesOnCancel { get; set; } = true;
    public AppTheme Theme { get; set; } = AppTheme.Dark;
}
