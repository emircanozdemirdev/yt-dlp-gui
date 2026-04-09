using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpGui.App.Models;
using YtDlpGui.App.Services;

namespace YtDlpGui.App.ViewModels;

public partial class QuickDownloadViewModel : ObservableObject
{
    private readonly IYtDlpService ytDlpService;
    private readonly IQueueService queueService;
    private readonly SettingsViewModel settingsViewModel;

    public QuickDownloadViewModel(
        IYtDlpService ytDlpService,
        IQueueService queueService,
        SettingsViewModel settingsViewModel)
    {
        this.ytDlpService = ytDlpService;
        this.queueService = queueService;
        this.settingsViewModel = settingsViewModel;
    }

    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string uploader = string.Empty;

    [ObservableProperty]
    private string duration = "-";

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string statusMessage = "Ready.";

    [ObservableProperty]
    private FormatOption? selectedFormat;

    [ObservableProperty]
    private string quickPreset = "Best";

    public ObservableCollection<FormatOption> Formats { get; } = [];
    public IReadOnlyList<string> Presets { get; } = ["Best", "1080p", "Audio Only"];

    [RelayCommand]
    private async Task AnalyzeAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            StatusMessage = "URL is required.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Analyzing...";
        try
        {
            var metadata = await ytDlpService.AnalyzeAsync(Url.Trim(), settingsViewModel.Current, CancellationToken.None);
            Title = metadata.Title;
            Uploader = metadata.Uploader;
            Duration = metadata.Duration?.ToString(@"hh\:mm\:ss") ?? "-";

            Formats.Clear();
            foreach (var format in FormatListSorter.OrderHighToLow(metadata.Formats))
            {
                Formats.Add(format);
            }

            SelectedFormat = Formats.FirstOrDefault();
            StatusMessage = $"Found {Formats.Count} formats.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EnqueueDownloadAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            StatusMessage = "URL is required.";
            return;
        }

        var formatId = SelectedFormat?.FormatId;
        if (string.IsNullOrWhiteSpace(formatId))
        {
            formatId = GetPresetFormat(QuickPreset);
        }

        var job = new DownloadJob
        {
            Url = Url.Trim(),
            Title = Title,
            OutputDirectory = settingsViewModel.Current.OutputDirectory,
            SelectedFormatId = formatId
        };

        await queueService.EnqueueAsync(job);
        StatusMessage = "Added to queue.";
    }

    private static string GetPresetFormat(string preset) =>
        preset switch
        {
            "1080p" => "bestvideo[height<=1080]+bestaudio/best[height<=1080]",
            "Audio Only" => "bestaudio",
            _ => "best"
        };
}
