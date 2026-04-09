using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

public static class FormatListSorter
{
    /// <summary>
    /// Video-containing formats first (height high → low, then bitrate), then audio-only (bitrate high → low).
    /// </summary>
    public static List<FormatOption> OrderHighToLow(IReadOnlyList<FormatOption> formats)
    {
        var idComparer = StringComparer.Ordinal;

        var videos = formats
            .Where(f => !f.IsAudioOnly)
            .OrderByDescending(f => f.SortHeight)
            .ThenByDescending(f => f.SortBitrateKbps)
            .ThenBy(f => f.FormatId, idComparer);

        var audios = formats
            .Where(f => f.IsAudioOnly)
            .OrderByDescending(f => f.SortBitrateKbps)
            .ThenByDescending(f => f.SortHeight)
            .ThenBy(f => f.FormatId, idComparer);

        return videos.Concat(audios).ToList();
    }
}
