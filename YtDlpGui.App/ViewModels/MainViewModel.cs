using CommunityToolkit.Mvvm.ComponentModel;
using YtDlpGui.App.Services;

namespace YtDlpGui.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public MainViewModel(
        IYtDlpService ytDlpService,
        IQueueService queueService,
        ISettingsService settingsService,
        IHistoryService historyService,
        IThemeService themeService)
    {
        Settings = new SettingsViewModel(settingsService, themeService);
        QuickDownload = new QuickDownloadViewModel(ytDlpService, queueService, Settings);
        Queue = new QueueViewModel(queueService);
        History = new HistoryViewModel(historyService);
    }

    public async Task InitializeAsync()
    {
        await Settings.LoadAsync();
        await History.LoadAsync();
    }

    public QuickDownloadViewModel QuickDownload { get; }
    public QueueViewModel Queue { get; }
    public HistoryViewModel History { get; }
    public SettingsViewModel Settings { get; }
}
