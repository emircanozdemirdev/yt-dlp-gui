using System.Windows;
using YtDlpGui.App.Infrastructure;
using YtDlpGui.App.Services;
using YtDlpGui.App.ViewModels;

namespace YtDlpGui.App;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var processRunner = new ProcessRunner();
            var progressParser = new ProgressParser();
            var ytDlpService = new YtDlpService(processRunner, progressParser);
            var settingsService = new SettingsService();
            var historyService = new HistoryService();
            var queueService = new QueueService(ytDlpService, settingsService, historyService);

            var mainVm = new MainViewModel(ytDlpService, queueService, settingsService, historyService);
            await mainVm.InitializeAsync();

            var mainWindow = new MainWindow
            {
                DataContext = mainVm
            };

            mainWindow.Show();
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
}
