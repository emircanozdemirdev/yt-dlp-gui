using System.Globalization;
using System.Text.Json;
using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

internal static class AppSettingsJson
{
    internal static AppSettings DeserializeSafe(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new AppSettings();
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return MapFromRoot(doc.RootElement);
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    private static AppSettings MapFromRoot(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return new AppSettings();
        }

        var s = new AppSettings();

        if (TryGetString(root, nameof(AppSettings.YtDlpPath), out var ytdlp))
        {
            s.YtDlpPath = ytdlp;
        }

        if (TryGetString(root, nameof(AppSettings.FfmpegPath), out var ffmpeg))
        {
            s.FfmpegPath = ffmpeg;
        }

        if (TryGetString(root, nameof(AppSettings.OutputDirectory), out var output))
        {
            s.OutputDirectory = output;
        }

        if (TryGetString(root, nameof(AppSettings.FileNameTemplate), out var template))
        {
            s.FileNameTemplate = template;
        }

        if (TryGetInt32(root, nameof(AppSettings.MaxParallelDownloads), out var maxParallel))
        {
            s.MaxParallelDownloads = maxParallel;
        }

        if (TryGetInt32(root, nameof(AppSettings.Retries), out var retries))
        {
            s.Retries = retries;
        }

        if (TryGetString(root, nameof(AppSettings.Proxy), out var proxy))
        {
            s.Proxy = string.IsNullOrEmpty(proxy) ? null : proxy;
        }
        else if (TryGetNull(root, nameof(AppSettings.Proxy)))
        {
            s.Proxy = null;
        }

        if (TryGetNullableInt32(root, nameof(AppSettings.RateLimitKbps), out var rate))
        {
            s.RateLimitKbps = rate;
        }

        if (TryGetBoolean(root, nameof(AppSettings.DeleteTempFilesOnCancel), out var deleteTempFilesOnCancel))
        {
            s.DeleteTempFilesOnCancel = deleteTempFilesOnCancel;
        }

        if (TryGetBoolean(root, nameof(AppSettings.BandwidthScheduleEnabled), out var bandwidthScheduleEnabled))
        {
            s.BandwidthScheduleEnabled = bandwidthScheduleEnabled;
        }

        if (TryGetNullableInt32(root, nameof(AppSettings.BandwidthLimitKbps), out var bandwidthLimitKbps))
        {
            s.BandwidthLimitKbps = bandwidthLimitKbps;
        }

        if (TryGetString(root, nameof(AppSettings.BandwidthWindowStart), out var bandwidthWindowStart))
        {
            s.BandwidthWindowStart = bandwidthWindowStart;
        }

        if (TryGetString(root, nameof(AppSettings.BandwidthWindowEnd), out var bandwidthWindowEnd))
        {
            s.BandwidthWindowEnd = bandwidthWindowEnd;
        }

        if (TryGetDuplicatePolicy(root, nameof(AppSettings.DuplicatePolicy), out var duplicatePolicy))
        {
            s.DuplicatePolicy = duplicatePolicy;
        }

        if (TryGetBoolean(root, nameof(AppSettings.NotifyOnCompletion), out var notifyOnCompletion))
        {
            s.NotifyOnCompletion = notifyOnCompletion;
        }

        if (TryGetBoolean(root, nameof(AppSettings.PlaySoundOnCompletion), out var playSoundOnCompletion))
        {
            s.PlaySoundOnCompletion = playSoundOnCompletion;
        }

        if (TryGetBoolean(root, nameof(AppSettings.EnablePlaylistDownload), out var enablePlaylistDownload))
        {
            s.EnablePlaylistDownload = enablePlaylistDownload;
        }

        if (TryGetString(root, nameof(AppSettings.DownloadArchivePath), out var downloadArchivePath))
        {
            s.DownloadArchivePath = downloadArchivePath;
        }

        if (TryGetString(root, nameof(AppSettings.SelectedProfileId), out var selectedProfileId))
        {
            s.SelectedProfileId = selectedProfileId;
        }

        if (TryGetDownloadProfiles(root, nameof(AppSettings.DownloadProfiles), out var profiles))
        {
            s.DownloadProfiles = profiles;
        }

        if (TryGetTheme(root, nameof(AppSettings.Theme), out var theme))
        {
            s.Theme = theme;
        }

        NormalizeProfiles(s);

        return s;
    }

    private static bool TryGetPropertyCi(JsonElement root, string name, out JsonElement value)
    {
        foreach (var prop in root.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static bool TryGetString(JsonElement root, string name, out string value)
    {
        if (!TryGetPropertyCi(root, name, out var el) || el.ValueKind != JsonValueKind.String)
        {
            value = string.Empty;
            return false;
        }

        value = el.GetString() ?? string.Empty;
        return true;
    }

    private static bool TryGetNull(JsonElement root, string name)
    {
        return TryGetPropertyCi(root, name, out var el) && el.ValueKind == JsonValueKind.Null;
    }

    private static bool TryGetInt32(JsonElement root, string name, out int value)
    {
        value = default;
        if (!TryGetPropertyCi(root, name, out var el))
        {
            return false;
        }

        if (el.ValueKind == JsonValueKind.Null)
        {
            return false;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out value))
        {
            return true;
        }

        if (el.ValueKind == JsonValueKind.String &&
            int.TryParse(el.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        return false;
    }

    private static bool TryGetNullableInt32(JsonElement root, string name, out int? value)
    {
        value = null;
        if (!TryGetPropertyCi(root, name, out var el))
        {
            return false;
        }

        if (el.ValueKind == JsonValueKind.Null)
        {
            value = null;
            return true;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n))
        {
            value = n;
            return true;
        }

        if (el.ValueKind == JsonValueKind.String &&
            int.TryParse(el.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryGetBoolean(JsonElement root, string name, out bool value)
    {
        value = default;
        if (!TryGetPropertyCi(root, name, out var el))
        {
            return false;
        }

        if (el.ValueKind == JsonValueKind.True || el.ValueKind == JsonValueKind.False)
        {
            value = el.GetBoolean();
            return true;
        }

        if (el.ValueKind == JsonValueKind.String && bool.TryParse(el.GetString(), out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryGetDuplicatePolicy(JsonElement root, string name, out DuplicatePolicy value)
    {
        value = DuplicatePolicy.Allow;
        if (!TryGetPropertyCi(root, name, out var el))
        {
            return false;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var numeric))
        {
            if (Enum.IsDefined(typeof(DuplicatePolicy), numeric))
            {
                value = (DuplicatePolicy)numeric;
                return true;
            }

            return false;
        }

        if (el.ValueKind == JsonValueKind.String &&
            Enum.TryParse<DuplicatePolicy>(el.GetString(), true, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryGetDownloadProfiles(JsonElement root, string name, out List<DownloadProfile> profiles)
    {
        profiles = [];
        if (!TryGetPropertyCi(root, name, out var el) || el.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var item in el.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var profile = new DownloadProfile();
            if (TryGetString(item, nameof(DownloadProfile.Id), out var id))
            {
                profile.Id = id;
            }

            if (TryGetString(item, nameof(DownloadProfile.DisplayName), out var displayName))
            {
                profile.DisplayName = displayName;
            }

            if (TryGetString(item, nameof(DownloadProfile.FormatSelector), out var formatSelector))
            {
                profile.FormatSelector = formatSelector;
            }

            if (TryGetBoolean(item, nameof(DownloadProfile.UseDownloadArchive), out var useDownloadArchive))
            {
                profile.UseDownloadArchive = useDownloadArchive;
            }

            profiles.Add(profile);
        }

        return true;
    }

    private static void NormalizeProfiles(AppSettings settings)
    {
        settings.DownloadProfiles =
            settings.DownloadProfiles
                .Where(p =>
                    !string.IsNullOrWhiteSpace(p.Id) &&
                    !string.IsNullOrWhiteSpace(p.DisplayName) &&
                    !string.IsNullOrWhiteSpace(p.FormatSelector))
                .ToList();

        if (settings.DownloadProfiles.Count == 0)
        {
            settings.DownloadProfiles = [.. DownloadProfilesCatalog.Defaults];
        }

        if (settings.DownloadProfiles.All(x => !string.Equals(x.Id, settings.SelectedProfileId, StringComparison.OrdinalIgnoreCase)))
        {
            settings.SelectedProfileId = settings.DownloadProfiles[0].Id;
        }
    }

    private static bool TryGetTheme(JsonElement root, string name, out AppTheme value)
    {
        value = AppTheme.Dark;
        if (!TryGetPropertyCi(root, name, out var el))
        {
            return false;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var numeric))
        {
            if (Enum.IsDefined(typeof(AppTheme), numeric))
            {
                value = (AppTheme)numeric;
                return true;
            }

            return false;
        }

        if (el.ValueKind == JsonValueKind.String &&
            Enum.TryParse<AppTheme>(el.GetString(), true, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }
}
