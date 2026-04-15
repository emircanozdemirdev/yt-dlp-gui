using CommunityToolkit.Mvvm.ComponentModel;
using YtDlpGui.App.Services;

namespace YtDlpGui.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IQueueService queueService;

    public MainViewModel(
        IYtDlpService ytDlpService,
        IQueueService queueService,
        ISettingsService settingsService,
        IHistoryService historyService,
        IThemeService themeService)
    {
        this.queueService = queueService;
        Settings = new SettingsViewModel(settingsService, themeService);
        QuickDownload = new QuickDownloadViewModel(ytDlpService, queueService, Settings);
        PlaylistDownload = new PlaylistDownloadViewModel(queueService, Settings);
        Queue = new QueueViewModel(queueService);
        History = new HistoryViewModel(historyService, queueService);
    }

    public async Task InitializeAsync()
    {
        await queueService.LoadPersistedJobsAsync();
        await Settings.LoadAsync();
        QuickDownload.RefreshProfilesFromSettings();
        await History.LoadAsync();
    }

    public QuickDownloadViewModel QuickDownload { get; }
    public PlaylistDownloadViewModel PlaylistDownload { get; }
    public QueueViewModel Queue { get; }
    public HistoryViewModel History { get; }
    public SettingsViewModel Settings { get; }
}
