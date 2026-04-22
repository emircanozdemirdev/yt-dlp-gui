using System.Windows;
using YtDlpGui.App.Infrastructure;
using YtDlpGui.App.Services;
using YtDlpGui.App.ViewModels;

namespace YtDlpGui.App;

public partial class App : Application
{
    private IQueueService? queueService;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            await ToolBootstrapper.EnsureToolsPresentAsync();

            var processRunner = new ProcessRunner();
            var progressParser = new ProgressParser();
            var ytDlpService = new YtDlpService(processRunner, progressParser);
            var settingsService = new SettingsService();
            var historyService = new HistoryService();
            var uiDispatcher = new WpfUiDispatcher();
            var userPromptService = new DesktopUserPromptService();
            var notificationService = new DesktopNotificationService();
            queueService = new QueueService(
                ytDlpService,
                settingsService,
                historyService,
                uiDispatcher,
                userPromptService,
                notificationService);
            var themeService = new ThemeService();
            var settings = await settingsService.LoadAsync();

            // Eğer kullanıcı daha önce özel bir yol belirtmemişse,
            // bootstrapper'ın indirdiği araçları kullan.
            var toolsDir = ToolBootstrapper.UserToolsDirectory;
            if (string.Equals(settings.YtDlpPath, "yt-dlp", StringComparison.OrdinalIgnoreCase))
            {
                settings.YtDlpPath = Path.Combine(toolsDir, "yt-dlp.exe");
            }
            if (string.Equals(settings.FfmpegPath, "ffmpeg", StringComparison.OrdinalIgnoreCase))
            {
                settings.FfmpegPath = Path.Combine(toolsDir, "ffmpeg.exe");
            }

            themeService.Apply(settings.Theme);

            var mainVm = new MainViewModel(
                ytDlpService,
                this.queueService,
                settingsService,
                historyService,
                themeService);
            await mainVm.InitializeAsync();

            var mainWindow = new MainWindow
            {
                DataContext = mainVm
            };

            mainWindow.Show();
            themeService.Apply(settings.Theme);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Gerekli araçlar hazırlanırken bir hata oluştu. İnternet bağlantınızı kontrol edin.\n\n{ex.Message}",
                "yt-dlp GUI",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            queueService?.PersistAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Best-effort persistence on app close.
        }

        base.OnExit(e);
    }
}
