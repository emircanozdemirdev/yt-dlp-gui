using System.IO.Compression;
using System.Net.Http;

namespace YtDlpGui.App.Services;

public static class ToolBootstrapper
{
    private const string YtDlpDownloadUrl =
        "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";

    private const string FfmpegZipUrl =
        "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";

    private static readonly HttpClient SharedHttp = CreateHttpClient();

    public static string UserToolsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "YtDlpGui", "tools");

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(45);
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "YtDlpGui/1.0 (+https://github.com/)");
        return client;
    }

    /// <summary>
    /// Ensures yt-dlp.exe and ffmpeg.exe exist under LocalAppData/YtDlpGui/tools,
    /// copying from app tools/ if present or downloading from official releases.
    /// </summary>
    public static async Task EnsureToolsPresentAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(UserToolsDirectory);
        var bundledDir = Path.Combine(AppContext.BaseDirectory, "tools");

        var ytdlpDest = Path.Combine(UserToolsDirectory, "yt-dlp.exe");
        if (!File.Exists(ytdlpDest))
        {
            var bundled = Path.Combine(bundledDir, "yt-dlp.exe");
            if (File.Exists(bundled))
            {
                File.Copy(bundled, ytdlpDest, overwrite: true);
            }
            else
            {
                await DownloadFileAsync(YtDlpDownloadUrl, ytdlpDest, cancellationToken);
            }
        }

        var ffmpegDest = Path.Combine(UserToolsDirectory, "ffmpeg.exe");
        if (!File.Exists(ffmpegDest))
        {
            var bundled = Path.Combine(bundledDir, "ffmpeg.exe");
            if (File.Exists(bundled))
            {
                File.Copy(bundled, ffmpegDest, overwrite: true);
            }
            else
            {
                await DownloadAndExtractFfmpegAsync(ffmpegDest, cancellationToken);
            }
        }
    }

    private static async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        await using var response = await SharedHttp.GetStreamAsync(url, cancellationToken);
        await using var fs = new FileStream(
            destinationPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
        await response.CopyToAsync(fs, cancellationToken);
    }

    private static async Task DownloadAndExtractFfmpegAsync(string destinationExePath, CancellationToken cancellationToken)
    {
        var zipPath = Path.Combine(Path.GetTempPath(), $"yt-dlp-gui-ffmpeg-{Guid.NewGuid():N}.zip");
        var extractDir = Path.Combine(Path.GetTempPath(), $"yt-dlp-gui-ffmpeg-ex-{Guid.NewGuid():N}");

        try
        {
            await DownloadFileAsync(FfmpegZipUrl, zipPath, cancellationToken);
            Directory.CreateDirectory(extractDir);
            ZipFile.ExtractToDirectory(zipPath, extractDir);

            var found = Directory.GetFiles(extractDir, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (found is null || !File.Exists(found))
            {
                throw new InvalidOperationException("ffmpeg.exe was not found inside the downloaded archive.");
            }

            File.Copy(found, destinationExePath, overwrite: true);
        }
        finally
        {
            TryDelete(zipPath);
            TryDeleteDirectory(extractDir);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore cleanup failures.
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failures.
        }
    }
}
