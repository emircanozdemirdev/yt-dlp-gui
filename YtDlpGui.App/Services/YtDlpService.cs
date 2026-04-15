using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using YtDlpGui.App.Infrastructure;
using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

public sealed class YtDlpService(IProcessRunner processRunner, ProgressParser progressParser) : IYtDlpService
{
    private static readonly Regex PlaylistItemRegex = new(
        @"Downloading item (?<index>\d+) of (?<total>\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public async Task<VideoMetadata> AnalyzeAsync(string url, AppSettings settings, CancellationToken cancellationToken)
    {
        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        var playlistArg = settings.EnablePlaylistDownload ? string.Empty : "--no-playlist ";
        var args = $"-J {playlistArg}\"{url}\"";
        var exitCode = await processRunner.RunAsync(
            settings.YtDlpPath,
            args,
            line => outputBuilder.AppendLine(line),
            line => errorBuilder.AppendLine(line),
            cancellationToken);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(errorBuilder.ToString())
                ? "Failed to analyze URL."
                : errorBuilder.ToString());
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(outputBuilder.ToString());
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "yt-dlp returned invalid JSON. Check the URL and that yt-dlp is up to date.",
                ex);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                root = root[0];
            }

            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("Unexpected yt-dlp JSON: expected an object.");
            }

            var formats = new List<FormatOption>();
            if (root.TryGetProperty("formats", out var formatsElement) && formatsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in formatsElement.EnumerateArray())
                {
                    var formatId = ResolveFormatId(item);
                    if (string.IsNullOrWhiteSpace(formatId))
                    {
                        continue;
                    }

                    var vcodec = YtDlpJsonReader.GetString(item, "vcodec", "unknown");
                    var acodec = YtDlpJsonReader.GetString(item, "acodec", "unknown");
                    var resolution = YtDlpJsonReader.GetString(item, "resolution", "-");
                    var ext = YtDlpJsonReader.GetString(item, "ext", "-");
                    var tbr = YtDlpJsonReader.FormatBitrateK(item);
                    var size = YtDlpJsonReader.GetFileSizeBytes(item);
                    var height = YtDlpJsonReader.GetInt32(item, "height") ?? 0;
                    var tbrNum = YtDlpJsonReader.GetDouble(item, "tbr");
                    var abrNum = YtDlpJsonReader.GetDouble(item, "abr");
                    var vbrNum = YtDlpJsonReader.GetDouble(item, "vbr");
                    var sortBitrate = MaxKbps(tbrNum, abrNum, vbrNum);
                    var isAudioOnly = vcodec == "none";

                    formats.Add(new FormatOption
                    {
                        FormatId = formatId,
                        Extension = ext,
                        Resolution = resolution,
                        Codec = $"{vcodec}/{acodec}",
                        Bitrate = tbr,
                        FileSizeBytes = size,
                        IsAudioOnly = isAudioOnly,
                        SortHeight = isAudioOnly ? 0 : height,
                        SortBitrateKbps = sortBitrate
                    });
                }
            }

            var durationSeconds = YtDlpJsonReader.GetDouble(root, "duration");
            var isPlaylist = string.Equals(
                YtDlpJsonReader.GetString(root, "_type", string.Empty),
                "playlist",
                StringComparison.OrdinalIgnoreCase);
            var playlistItemCount = 0;
            if (root.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array)
            {
                playlistItemCount = entries.GetArrayLength();
            }
            return new VideoMetadata
            {
                Url = url,
                Title = YtDlpJsonReader.GetString(root, "title", "Unknown title"),
                Uploader = YtDlpJsonReader.GetString(root, "uploader", "-"),
                Duration = durationSeconds is null ? null : TimeSpan.FromSeconds(durationSeconds.Value),
                IsPlaylist = isPlaylist,
                PlaylistItemCount = playlistItemCount,
                Formats = formats
            };
        }
    }

    private static double MaxKbps(params double?[] values)
    {
        double max = 0;
        foreach (var v in values)
        {
            if (v is > 0 && v.Value > max)
            {
                max = v.Value;
            }
        }

        return max;
    }

    private static string? ResolveFormatId(JsonElement item)
    {
        if (!item.TryGetProperty("format_id", out var idProp) || idProp.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (idProp.ValueKind == JsonValueKind.String)
        {
            return idProp.GetString();
        }

        if (idProp.ValueKind == JsonValueKind.Number && idProp.TryGetInt32(out var asInt))
        {
            return asInt.ToString(CultureInfo.InvariantCulture);
        }

        return null;
    }

    public async Task DownloadAsync(
        DownloadJob job,
        AppSettings settings,
        IProgress<ProgressUpdate> progress,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(job.OutputDirectory);
        if (string.IsNullOrWhiteSpace(job.TemporaryDirectory))
        {
            job.TemporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "YtDlpGui",
                "downloads",
                job.Id.ToString("N"));
        }
        Directory.CreateDirectory(job.TemporaryDirectory);
        var formatArg = string.IsNullOrWhiteSpace(job.SelectedFormatId) ? "best" : job.SelectedFormatId;
        var continueArg = job.ResumePartialDownload ? "--continue " : "--no-continue ";
        var playlistArg = job.IsPlaylist ? "--yes-playlist " : "--no-playlist ";
        var rateLimit = job.EffectiveRateLimitKbps ?? settings.RateLimitKbps;
        var archiveArg = job.UseDownloadArchive
            ? $"--download-archive \"{settings.DownloadArchivePath}\" "
            : string.Empty;

        var args =
            $"-f \"{formatArg}\" --newline --retries {settings.Retries} {continueArg}" +
            playlistArg +
            archiveArg +
            $"--paths \"temp:{job.TemporaryDirectory}\" " +
            $"{(string.IsNullOrWhiteSpace(settings.Proxy) ? string.Empty : $"--proxy \"{settings.Proxy}\" ")}" +
            $"{(rateLimit is null ? string.Empty : $"--limit-rate {rateLimit}K ")}" +
            $"--ffmpeg-location \"{settings.FfmpegPath}\" " +
            $"-o \"{Path.Combine(job.OutputDirectory, settings.FileNameTemplate)}\" " +
            $"\"{job.Url}\"";

        var exitCode = await processRunner.RunAsync(
            settings.YtDlpPath,
            args,
            line =>
            {
                CaptureOutputPath(job, line);
                var parsed = progressParser.Parse(line);
                if (parsed is not null)
                {
                    progress.Report(parsed);
                }
            },
            line =>
            {
                CaptureOutputPath(job, line);
                // Avoid flooding UI bindings with noisy stderr lines.
                if (!string.IsNullOrWhiteSpace(line) &&
                    !line.StartsWith("[download]", StringComparison.OrdinalIgnoreCase))
                {
                    job.Message = line;
                }
            },
            cancellationToken);

        if (exitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(job.Message)
                ? "Download failed."
                : job.Message);
        }
    }

    private static void CaptureOutputPath(DownloadJob job, string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        const string destinationPrefix = "[download] Destination: ";
        if (line.StartsWith(destinationPrefix, StringComparison.Ordinal))
        {
            var path = line[destinationPrefix.Length..].Trim().Trim('"');
            job.OutputFilePath = path;
            job.RegisterArtifactPath(path);
            if (job.IsPlaylist)
            {
                var item = Path.GetFileName(path);
                if (!string.Equals(job.CurrentPlaylistItem, item, StringComparison.Ordinal))
                {
                    job.CurrentPlaylistItem = item;
                }
            }
            return;
        }

        const string mergePrefix = "[Merger] Merging formats into ";
        if (line.StartsWith(mergePrefix, StringComparison.Ordinal))
        {
            var path = line[mergePrefix.Length..].Trim().Trim('"');
            job.OutputFilePath = path;
            job.RegisterArtifactPath(path);
            if (job.IsPlaylist)
            {
                var item = Path.GetFileName(path);
                if (!string.Equals(job.CurrentPlaylistItem, item, StringComparison.Ordinal))
                {
                    job.CurrentPlaylistItem = item;
                }
            }
            return;
        }

        if (job.IsPlaylist)
        {
            var match = PlaylistItemRegex.Match(line);
            if (match.Success)
            {
                var index = match.Groups["index"].Value;
                var total = match.Groups["total"].Value;
                if (int.TryParse(index, out var currentIndex))
                {
                    job.CurrentPlaylistIndex = currentIndex;
                }

                if (int.TryParse(total, out var totalCount))
                {
                    job.PlaylistItemCount = totalCount;
                }
            }
        }
    }
}
