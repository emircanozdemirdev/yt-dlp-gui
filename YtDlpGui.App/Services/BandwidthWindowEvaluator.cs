using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

public static class BandwidthWindowEvaluator
{
    public static int? ResolveEffectiveRateLimit(AppSettings settings)
    {
        if (settings.BandwidthScheduleEnabled)
        {
            return settings.BandwidthLimitKbps;
        }

        return settings.RateLimitKbps;
    }

    public static bool IsWithinWindow(AppSettings settings, DateTime now)
    {
        if (!settings.BandwidthScheduleEnabled)
        {
            return true;
        }

        if (!TimeOnly.TryParse(settings.BandwidthWindowStart, out var start) ||
            !TimeOnly.TryParse(settings.BandwidthWindowEnd, out var end))
        {
            return true;
        }

        var current = TimeOnly.FromDateTime(now);
        if (start <= end)
        {
            return current >= start && current <= end;
        }

        return current >= start || current <= end;
    }
}
