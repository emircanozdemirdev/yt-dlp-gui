using CommunityToolkit.Mvvm.ComponentModel;

namespace YtDlpGui.App.Models;

public partial class DownloadHistoryItem : ObservableObject
{
    [ObservableProperty]
    private Guid id = Guid.NewGuid();

    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string outputPath = string.Empty;

    [ObservableProperty]
    private DateTimeOffset completedAtUtc = DateTimeOffset.UtcNow;

    [ObservableProperty]
    private bool isSuccess;

    [ObservableProperty]
    private string message = string.Empty;

    [ObservableProperty]
    private bool isSelected;
}
