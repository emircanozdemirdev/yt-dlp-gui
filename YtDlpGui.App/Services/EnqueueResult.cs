namespace YtDlpGui.App.Services;

public sealed class EnqueueResult
{
    public bool Accepted { get; init; }
    public string Message { get; init; } = string.Empty;
}
