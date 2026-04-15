using System.Collections.ObjectModel;
using YtDlpGui.App.Infrastructure;
using YtDlpGui.App.Models;
using YtDlpGui.App.Services;
using YtDlpGui.App.ViewModels;

namespace YtDlpGui.App.Tests;

public class QueueAndHistoryViewModelTests
{
    [Fact]
    public void QueueViewModel_BulkActions_RespectSelectedStatuses()
    {
        var queueService = new FakeQueueService();
        queueService.Jobs.Add(new DownloadJob { Status = DownloadStatus.Pending, IsSelected = true });
        queueService.Jobs.Add(new DownloadJob { Status = DownloadStatus.Failed, IsSelected = true });

        var vm = new QueueViewModel(queueService);

        Assert.True(vm.HasSelection);
        Assert.True(vm.CanPauseResumeSelected);
        Assert.True(vm.CanCancelSelected);
        Assert.True(vm.CanRetrySelected);
    }

    [Fact]
    public async Task HistoryViewModel_OpenFolder_Enablement_TracksValidPaths()
    {
        var existingDir = Path.Combine(Path.GetTempPath(), $"yt-dlp-gui-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(existingDir);
        try
        {
            var historyService = new FakeHistoryService(
                new DownloadHistoryItem
                {
                    Title = "item",
                    OutputPath = Path.Combine(existingDir, "file.mp4"),
                    IsSelected = true
                });
            var vm = new HistoryViewModel(historyService, new FakeQueueService());

            await vm.LoadAsync();
            Assert.True(vm.CanOpenSelectedFolder);

            vm.Items[0].OutputPath = string.Empty;
            Assert.False(vm.CanOpenSelectedFolder);
        }
        finally
        {
            if (Directory.Exists(existingDir))
            {
                Directory.Delete(existingDir, recursive: true);
            }
        }
    }

    [Fact]
    public void QueueService_CancelPending_RemovesJobAndAddsHistory()
    {
        var history = new FakeHistoryService();
        var queueService = new QueueService(
            new FakeYtDlpService(),
            new FakeSettingsService(),
            history,
            new InlineUiDispatcher(),
            new FakeUserPromptService(),
            new FakeNotificationService());

        var job = new DownloadJob
        {
            Id = Guid.NewGuid(),
            Url = "https://example.com",
            Title = "Example",
            OutputDirectory = Path.GetTempPath(),
            Status = DownloadStatus.Pending
        };
        queueService.Jobs.Add(job);

        queueService.Cancel(job.Id);

        Assert.Empty(queueService.Jobs);
        Assert.Contains(history.Items, x => x.Url == job.Url && x.Message == "Canceled");
    }

    private sealed class FakeQueueService : IQueueService
    {
        public ObservableCollection<DownloadJob> Jobs { get; } = [];
        public Task LoadPersistedJobsAsync() => Task.CompletedTask;
        public Task PersistAsync() => Task.CompletedTask;
        public Task<EnqueueResult> EnqueueAsync(DownloadJob job)
        {
            Jobs.Add(job);
            return Task.FromResult(new EnqueueResult
            {
                Accepted = true,
                Message = "Added to queue."
            });
        }
        public void Pause(Guid id) { }
        public void Resume(Guid id) { }
        public void Retry(Guid id) { }
        public void Remove(Guid id) { }
        public void RetryFailed() { }
        public void Cancel(Guid id) { }
    }

    private sealed class FakeHistoryService(params DownloadHistoryItem[] seedItems) : IHistoryService
    {
        public List<DownloadHistoryItem> Items { get; } = [.. seedItems];

        public Task<IReadOnlyList<DownloadHistoryItem>> LoadAsync()
            => Task.FromResult<IReadOnlyList<DownloadHistoryItem>>(Items.ToList());

        public Task AddAsync(DownloadHistoryItem item)
        {
            Items.Add(item);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(IEnumerable<Guid> ids)
        {
            var idSet = ids.ToHashSet();
            Items.RemoveAll(x => idSet.Contains(x.Id));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeYtDlpService : IYtDlpService
    {
        public Task<VideoMetadata> AnalyzeAsync(string url, AppSettings settings, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task DownloadAsync(DownloadJob job, AppSettings settings, IProgress<ProgressUpdate> progress, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakeSettingsService : ISettingsService
    {
        public Task<AppSettings> LoadAsync() => Task.FromResult(new AppSettings());
        public Task SaveAsync(AppSettings settings) => Task.CompletedTask;
    }

    private sealed class InlineUiDispatcher : IUiDispatcher
    {
        public void Invoke(Action action) => action();
    }

    private sealed class FakeUserPromptService : IUserPromptService
    {
        public DuplicatePromptAction ResolveDuplicate(string url) => DuplicatePromptAction.AddAnyway;
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public void Notify(string title, string message) { }
        public void PlayCompletionSound() { }
    }
}
