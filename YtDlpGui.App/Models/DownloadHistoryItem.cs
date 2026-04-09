namespace YtDlpGui.App.Models;

public sealed class DownloadHistoryItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string OutputPath { get; set; } = string.Empty;
    public DateTimeOffset CompletedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}
