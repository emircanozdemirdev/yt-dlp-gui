namespace YtDlpGui.App.Models;

public sealed class FormatOption
{
    public string FormatId { get; init; } = string.Empty;
    public string Extension { get; init; } = string.Empty;
    public string Resolution { get; init; } = string.Empty;
    public string Codec { get; init; } = string.Empty;
    public string Bitrate { get; init; } = string.Empty;
    public long? FileSizeBytes { get; init; }
    public bool IsAudioOnly { get; init; }

    /// <summary>Video height in pixels from yt-dlp (0 if unknown / audio-only).</summary>
    public int SortHeight { get; init; }

    /// <summary>Best available bitrate hint in kbps (tbr / abr / vbr).</summary>
    public double SortBitrateKbps { get; init; }

    public string MediaKind => IsAudioOnly ? "Audio" : "Video";

    public string DisplayName =>
        $"{FormatId} | {Extension} | {Resolution} | {Codec} | {Bitrate}";
}
