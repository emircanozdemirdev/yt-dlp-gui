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
}
