using CommunityToolkit.Mvvm.ComponentModel;

namespace YtDlpGui.App.Models;

public partial class DownloadJob : ObservableObject
{
    [ObservableProperty]
    private Guid id = Guid.NewGuid();

    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string outputDirectory = string.Empty;

    [ObservableProperty]
    private string? selectedFormatId;

    [ObservableProperty]
    private DownloadStatus status = DownloadStatus.Pending;

    [ObservableProperty]
    private double progressPercent;

    [ObservableProperty]
    private string speedText = "-";

    [ObservableProperty]
    private string etaText = "-";

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private DateTimeOffset createdAtUtc = DateTimeOffset.UtcNow;

    [ObservableProperty]
    private bool resumePartialDownload;

    [ObservableProperty]
    private string? outputFilePath;

    [ObservableProperty]
    private string? temporaryDirectory;

    [ObservableProperty]
    private bool isPlaylist;

    [ObservableProperty]
    private int? effectiveRateLimitKbps;

    [ObservableProperty]
    private bool useDownloadArchive;

    [ObservableProperty]
    private string currentPlaylistItem = "-";

    [ObservableProperty]
    private int playlistItemCount;

    [ObservableProperty]
    private int currentPlaylistIndex;

    [ObservableProperty]
    private double currentItemProgressPercent;

    private readonly HashSet<string> artifactPaths = [];

    [ObservableProperty]
    private bool isSelected;

    public bool CanPauseResume =>
        Status is DownloadStatus.Pending or DownloadStatus.Running or DownloadStatus.Paused;
    public bool CanCancel => Status is DownloadStatus.Pending or DownloadStatus.Running or DownloadStatus.Paused;
    public string PauseResumeActionText => Status == DownloadStatus.Paused ? "Resume" : "Pause";
    public string PlaylistProgressText =>
        IsPlaylist && PlaylistItemCount > 0 && CurrentPlaylistIndex > 0
            ? $"{CurrentPlaylistIndex}/{PlaylistItemCount}"
            : "-";
    public string ListProgressDisplay =>
        IsPlaylist
            ? PlaylistProgressText
            : "-";
    public double ActiveItemProgressPercent =>
        IsPlaylist
            ? CurrentItemProgressPercent
            : ProgressPercent;

    partial void OnStatusChanged(DownloadStatus value)
    {
        OnPropertyChanged(nameof(CanPauseResume));
        OnPropertyChanged(nameof(CanCancel));
        OnPropertyChanged(nameof(PauseResumeActionText));
    }

    partial void OnPlaylistItemCountChanged(int value)
    {
        OnPropertyChanged(nameof(PlaylistProgressText));
        OnPropertyChanged(nameof(ListProgressDisplay));
    }

    partial void OnCurrentPlaylistIndexChanged(int value)
    {
        OnPropertyChanged(nameof(PlaylistProgressText));
        OnPropertyChanged(nameof(ListProgressDisplay));
    }

    partial void OnIsPlaylistChanged(bool value)
    {
        OnPropertyChanged(nameof(PlaylistProgressText));
        OnPropertyChanged(nameof(ListProgressDisplay));
        OnPropertyChanged(nameof(ActiveItemProgressPercent));
    }

    partial void OnProgressPercentChanged(double value) => OnPropertyChanged(nameof(ActiveItemProgressPercent));
    partial void OnCurrentItemProgressPercentChanged(double value) => OnPropertyChanged(nameof(ActiveItemProgressPercent));

    public void RegisterArtifactPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var normalized = path.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        lock (artifactPaths)
        {
            artifactPaths.Add(normalized);
        }
    }

    public IReadOnlyList<string> GetArtifactPathsSnapshot()
    {
        lock (artifactPaths)
        {
            return artifactPaths.ToList();
        }
    }
}
