using System.Text.Json;

namespace YtDlpGui.App.Services;

public static class YtDlpJsonReader
{
    public static string GetString(JsonElement parent, string propertyName, string fallback)
    {
        if (!parent.TryGetProperty(propertyName, out var el))
        {
            return fallback;
        }

        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString() ?? fallback,
            JsonValueKind.Null => fallback,
            _ => fallback
        };
    }

    public static double? GetDouble(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var el) || el.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out var value))
        {
            return value;
        }

        return null;
    }

    public static long? GetInt64(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var el) || el.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out var value))
        {
            return value;
        }

        return null;
    }

    public static int? GetInt32(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var el) || el.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var value))
        {
            return value;
        }

        return null;
    }

    public static string FormatBitrateK(JsonElement formatElement)
    {
        var tbr = GetDouble(formatElement, "tbr");
        if (tbr is not null)
        {
            return $"{tbr.Value:0}k";
        }

        return "-";
    }

    public static long? GetFileSizeBytes(JsonElement formatElement)
    {
        var exact = GetInt64(formatElement, "filesize");
        if (exact is not null)
        {
            return exact;
        }

        return GetInt64(formatElement, "filesize_approx");
    }
}
