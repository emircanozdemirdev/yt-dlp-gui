using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

public static class ToolPathResolver
{
    public static bool ApplyToolPaths(AppSettings settings)
    {
        var bundledDir = Path.Combine(AppContext.BaseDirectory, "tools");
        var userDir = ToolBootstrapper.UserToolsDirectory;

        var ytdlpBundled = Path.Combine(bundledDir, "yt-dlp.exe");
        var ytdlpUser = Path.Combine(userDir, "yt-dlp.exe");
        var ffmpegBundled = Path.Combine(bundledDir, "ffmpeg.exe");
        var ffmpegUser = Path.Combine(userDir, "ffmpeg.exe");

        var changed = false;

        if (ShouldReplaceYtDlp(settings.YtDlpPath))
        {
            var picked = PickFirstExisting(ytdlpBundled, ytdlpUser);
            if (picked is not null)
            {
                settings.YtDlpPath = picked;
                changed = true;
            }
        }

        if (ShouldReplaceFfmpeg(settings.FfmpegPath))
        {
            var picked = PickFirstExisting(ffmpegBundled, ffmpegUser);
            if (picked is not null)
            {
                settings.FfmpegPath = picked;
                changed = true;
            }
        }

        return changed;
    }

    private static string? PickFirstExisting(params string[] candidates) =>
        candidates.FirstOrDefault(File.Exists);

    private static bool ShouldReplaceYtDlp(string path) =>
        string.IsNullOrWhiteSpace(path) ||
        string.Equals(path, "yt-dlp", StringComparison.OrdinalIgnoreCase) ||
        IsMissingExecutablePath(path);

    private static bool ShouldReplaceFfmpeg(string path) =>
        string.IsNullOrWhiteSpace(path) ||
        string.Equals(path, "ffmpeg", StringComparison.OrdinalIgnoreCase) ||
        IsMissingExecutablePath(path);

    private static bool IsMissingExecutablePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        if (!Path.IsPathRooted(path))
        {
            return false;
        }

        return !File.Exists(path);
    }
}
