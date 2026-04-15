using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpGui.App.Models;
using YtDlpGui.App.Services;

namespace YtDlpGui.App.ViewModels;

public partial class PlaylistDownloadViewModel : ObservableObject
{
    private readonly IQueueService queueService;
    private readonly SettingsViewModel settingsViewModel;

    public PlaylistDownloadViewModel(
        IQueueService queueService,
        SettingsViewModel settingsViewModel)
    {
        this.queueService = queueService;
        this.settingsViewModel = settingsViewModel;
        QualityPresets =
        [
            new PlaylistQualityPreset
            {
                Name = "HD (up to 1080p)",
                FormatSelector = "bestvideo[height<=1080]+bestaudio/best[height<=1080]"
            },
            new PlaylistQualityPreset
            {
                Name = "2K (up to 1440p)",
                FormatSelector = "bestvideo[height<=1440]+bestaudio/best[height<=1440]"
            },
            new PlaylistQualityPreset
            {
                Name = "4K (up to 2160p)",
                FormatSelector = "bestvideo[height<=2160]+bestaudio/best[height<=2160]"
            },
            new PlaylistQualityPreset
            {
                Name = "Highest Quality",
                FormatSelector = "bestvideo+bestaudio/best"
            },
            new PlaylistQualityPreset
            {
                Name = "Audio High Quality",
                FormatSelector = "bestaudio/best"
            },
            new PlaylistQualityPreset
            {
                Name = "Audio Medium Quality",
                FormatSelector = "bestaudio[abr<=128]/bestaudio/best"
            }
        ];
        SelectedQualityPreset = QualityPresets[0];
    }

    [ObservableProperty]
    private string url = string.Empty;

    [ObservableProperty]
    private PlaylistQualityPreset? selectedQualityPreset;

    [ObservableProperty]
    private string statusMessage = "Ready.";

    public IReadOnlyList<PlaylistQualityPreset> QualityPresets { get; }

    [RelayCommand]
    private async Task EnqueuePlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            StatusMessage = "Playlist URL is required.";
            return;
        }

        var profile = settingsViewModel.Current.DownloadProfiles
            .FirstOrDefault(x => string.Equals(x.Id, settingsViewModel.Current.SelectedProfileId, StringComparison.OrdinalIgnoreCase));

        var job = new DownloadJob
        {
            Url = Url.Trim(),
            Title = "Playlist Download",
            OutputDirectory = settingsViewModel.Current.OutputDirectory,
            SelectedFormatId = SelectedQualityPreset?.FormatSelector ?? profile?.FormatSelector ?? "best",
            IsPlaylist = true,
            UseDownloadArchive = profile?.UseDownloadArchive ?? false
        };

        var result = await queueService.EnqueueAsync(job);
        StatusMessage = result.Message;
    }

}
