using CommunityToolkit.Mvvm.ComponentModel;

namespace YtDlpGui.App.Models;

public partial class DownloadJob : ObservableObject
{
    [ObservableProperty]
    private Guid id = Guid.NewGuid();

    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string outputDirectory = string.Empty;

    [ObservableProperty]
    private string? selectedFormatId;

    [ObservableProperty]
    private DownloadStatus status = DownloadStatus.Pending;

    [ObservableProperty]
    private double progressPercent;

    [ObservableProperty]
    private string speedText = "-";

    [ObservableProperty]
    private string etaText = "-";

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private DateTimeOffset createdAtUtc = DateTimeOffset.UtcNow;
}
