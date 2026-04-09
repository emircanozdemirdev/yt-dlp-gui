using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

public interface IYtDlpService
{
    Task<VideoMetadata> AnalyzeAsync(string url, AppSettings settings, CancellationToken cancellationToken);
    Task DownloadAsync(
        DownloadJob job,
        AppSettings settings,
        IProgress<ProgressUpdate> progress,
        CancellationToken cancellationToken);
}
