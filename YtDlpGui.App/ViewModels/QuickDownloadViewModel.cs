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
        InitializeProfiles();
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
    private DownloadProfile? selectedProfile;

    public ObservableCollection<FormatOption> Formats { get; } = [];
    public ObservableCollection<DownloadProfile> Profiles { get; } = [];

    partial void OnSelectedProfileChanged(DownloadProfile? value)
    {
        if (value is null)
        {
            return;
        }

        settingsViewModel.Current.SelectedProfileId = value.Id;
    }

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
            if (metadata.IsPlaylist)
            {
                StatusMessage = "Playlist URL detected. Use the Playlist tab for better control.";
            }

            Formats.Clear();
            foreach (var format in FormatListSorter.OrderHighToLow(metadata.Formats))
            {
                Formats.Add(format);
            }

            SelectedFormat = Formats.FirstOrDefault();
            InitializeProfiles();
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
        InitializeProfiles();

        if (string.IsNullOrWhiteSpace(Url))
        {
            StatusMessage = "URL is required.";
            return;
        }

        var formatId = SelectedFormat?.FormatId;
        if (string.IsNullOrWhiteSpace(formatId))
        {
            formatId = SelectedProfile?.FormatSelector ?? "best";
        }

        var job = new DownloadJob
        {
            Url = Url.Trim(),
            Title = Title,
            OutputDirectory = settingsViewModel.Current.OutputDirectory,
            SelectedFormatId = formatId,
            IsPlaylist = false,
            UseDownloadArchive = SelectedProfile?.UseDownloadArchive ?? false
        };

        var result = await queueService.EnqueueAsync(job);
        StatusMessage = result.Message;
    }

    private void InitializeProfiles()
    {
        var selectedId = SelectedProfile?.Id ?? settingsViewModel.Current.SelectedProfileId;
        Profiles.Clear();
        foreach (var profile in settingsViewModel.Current.DownloadProfiles)
        {
            Profiles.Add(profile);
        }

        SelectedProfile =
            Profiles.FirstOrDefault(x => string.Equals(x.Id, selectedId, StringComparison.OrdinalIgnoreCase)) ??
            Profiles.FirstOrDefault();
    }

    public void RefreshProfilesFromSettings()
    {
        InitializeProfiles();
    }
}
