using System.Collections.ObjectModel;
using YtDlpGui.App.Infrastructure;
using YtDlpGui.App.Models;
using YtDlpGui.App.Services;

namespace YtDlpGui.App.Tests;

public class FeatureWorkflowTests
{
    [Fact]
    public async Task QueueService_SkipPolicy_RejectsDuplicateUrl()
    {
        var history = new FakeHistoryService(new DownloadHistoryItem { Url = "https://example.com/video" });
        var settings = new AppSettings { DuplicatePolicy = DuplicatePolicy.Skip };
        var service = new QueueService(
            new FakeYtDlpService(),
            new FakeSettingsService(settings),
            history,
            new InlineUiDispatcher(),
            new FakeUserPromptService(DuplicatePromptAction.AddAnyway),
            new FakeNotificationService());

        var result = await service.EnqueueAsync(new DownloadJob
        {
            Url = "https://example.com/video",
            Title = "same",
            OutputDirectory = Path.GetTempPath()
        });

        Assert.False(result.Accepted);
        Assert.Empty(service.Jobs);
    }

    [Fact]
    public async Task QueueService_ReplacePolicy_RemovesPendingDuplicate()
    {
        var history = new FakeHistoryService();
        var settings = new AppSettings { DuplicatePolicy = DuplicatePolicy.Replace };
        var service = new QueueService(
            new FakeYtDlpService(),
            new FakeSettingsService(settings),
            history,
            new InlineUiDispatcher(),
            new FakeUserPromptService(DuplicatePromptAction.AddAnyway),
            new FakeNotificationService());

        service.Jobs.Add(new DownloadJob
        {
            Url = "https://example.com/video",
            Title = "old",
            Status = DownloadStatus.Pending,
            OutputDirectory = Path.GetTempPath()
        });

        var result = await service.EnqueueAsync(new DownloadJob
        {
            Url = "https://example.com/video",
            Title = "new",
            OutputDirectory = Path.GetTempPath()
        });

        Assert.True(result.Accepted);
        Assert.Single(service.Jobs);
        Assert.Equal("new", service.Jobs[0].Title);
    }

    [Fact]
    public void BandwidthWindowEvaluator_HandlesOvernightWindow()
    {
        var settings = new AppSettings
        {
            BandwidthScheduleEnabled = true,
            BandwidthWindowStart = "23:00",
            BandwidthWindowEnd = "07:00"
        };

        var atNight = BandwidthWindowEvaluator.IsWithinWindow(settings, new DateTime(2026, 1, 1, 23, 30, 0));
        var atMorning = BandwidthWindowEvaluator.IsWithinWindow(settings, new DateTime(2026, 1, 2, 6, 30, 0));
        var atNoon = BandwidthWindowEvaluator.IsWithinWindow(settings, new DateTime(2026, 1, 2, 12, 0, 0));

        Assert.True(atNight);
        Assert.True(atMorning);
        Assert.False(atNoon);
    }

    [Fact]
    public async Task YtDlpService_UsesPlaylistAndArchiveFlags()
    {
        var runner = new CaptureProcessRunner();
        var service = new YtDlpService(runner, new ProgressParser());
        var settings = new AppSettings
        {
            YtDlpPath = "yt-dlp",
            FfmpegPath = "ffmpeg",
            OutputDirectory = Path.GetTempPath(),
            DownloadArchivePath = Path.Combine(Path.GetTempPath(), "archive.txt")
        };
        var job = new DownloadJob
        {
            Url = "https://example.com/playlist",
            OutputDirectory = Path.GetTempPath(),
            IsPlaylist = true,
            UseDownloadArchive = true,
            SelectedFormatId = "best"
        };

        await service.DownloadAsync(job, settings, new Progress<ProgressUpdate>(_ => { }), CancellationToken.None);

        Assert.Contains("--yes-playlist", runner.LastArguments);
        Assert.Contains("--download-archive", runner.LastArguments);
    }

    private sealed class CaptureProcessRunner : IProcessRunner
    {
        public string LastArguments { get; private set; } = string.Empty;

        public Task<int> RunAsync(string fileName, string arguments, Action<string>? onOutput, Action<string>? onError, CancellationToken cancellationToken)
        {
            LastArguments = arguments;
            return Task.FromResult(0);
        }
    }

    private sealed class FakeYtDlpService : IYtDlpService
    {
        public Task<VideoMetadata> AnalyzeAsync(string url, AppSettings settings, CancellationToken cancellationToken) =>
            Task.FromResult(new VideoMetadata());

        public Task DownloadAsync(DownloadJob job, AppSettings settings, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeHistoryService(params DownloadHistoryItem[] seedItems) : IHistoryService
    {
        private readonly List<DownloadHistoryItem> items = [.. seedItems];

        public Task<IReadOnlyList<DownloadHistoryItem>> LoadAsync() => Task.FromResult<IReadOnlyList<DownloadHistoryItem>>(items.ToList());
        public Task AddAsync(DownloadHistoryItem item)
        {
            items.Add(item);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(IEnumerable<Guid> ids)
        {
            var set = ids.ToHashSet();
            items.RemoveAll(x => set.Contains(x.Id));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSettingsService(AppSettings settings) : ISettingsService
    {
        public Task<AppSettings> LoadAsync() => Task.FromResult(settings);
        public Task SaveAsync(AppSettings settingsValue) => Task.CompletedTask;
    }

    private sealed class InlineUiDispatcher : IUiDispatcher
    {
        public void Invoke(Action action) => action();
    }

    private sealed class FakeUserPromptService(DuplicatePromptAction action) : IUserPromptService
    {
        public DuplicatePromptAction ResolveDuplicate(string url) => action;
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public void Notify(string title, string message) { }
        public void PlayCompletionSound() { }
    }
}
