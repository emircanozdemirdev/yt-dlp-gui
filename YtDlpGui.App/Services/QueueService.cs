using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

public sealed class QueueService(
    IYtDlpService ytDlpService,
    ISettingsService settingsService,
    IHistoryService historyService) : IQueueService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public ObservableCollection<DownloadJob> Jobs { get; } = [];

    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> runningJobs = new();
    private readonly SemaphoreSlim queueSignal = new(0);
    private readonly SemaphoreSlim persistLock = new(1, 1);
    private readonly string queuePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "YtDlpGui",
        "queue.json");
    private bool isProcessorStarted;
    private int currentRunningCount;

    public async Task LoadPersistedJobsAsync()
    {
        if (!File.Exists(queuePath))
        {
            return;
        }

        List<DownloadJob>? persisted;
        try
        {
            await using var stream = File.OpenRead(queuePath);
            persisted = await JsonSerializer.DeserializeAsync<List<DownloadJob>>(stream);
        }
        catch (JsonException)
        {
            return;
        }

        if (persisted is null || persisted.Count == 0)
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var job in persisted)
            {
                if (job.Status is DownloadStatus.Pending or DownloadStatus.Running)
                {
                    job.Status = DownloadStatus.Pending;
                    job.SpeedText = "-";
                    job.EtaText = "-";
                    job.Message = "Restored from previous session.";
                    Jobs.Add(job);
                }
            }
        });

        if (Jobs.Any())
        {
            queueSignal.Release();
            if (!isProcessorStarted)
            {
                isProcessorStarted = true;
                _ = Task.Run(ProcessLoopAsync);
            }
        }
    }

    public async Task PersistAsync()
    {
        await persistLock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(queuePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            List<DownloadJob> snapshot = [];
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var job in Jobs)
                {
                    if (job.Status is DownloadStatus.Pending or DownloadStatus.Running)
                    {
                        snapshot.Add(job);
                    }
                }
            });

            await using var stream = File.Create(queuePath);
            await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions);
        }
        finally
        {
            persistLock.Release();
        }
    }

    public async Task EnqueueAsync(DownloadJob job)
    {
        Application.Current.Dispatcher.Invoke(() => Jobs.Add(job));
        await PersistAsync();
        queueSignal.Release();

        if (!isProcessorStarted)
        {
            isProcessorStarted = true;
            _ = Task.Run(ProcessLoopAsync);
        }

        await Task.CompletedTask;
    }

    public void Cancel(Guid id)
    {
        if (runningJobs.TryGetValue(id, out var cts))
        {
            cts.Cancel();
            return;
        }

        var pending = Jobs.FirstOrDefault(x => x.Id == id && x.Status == DownloadStatus.Pending);
        if (pending is not null)
        {
            pending.Status = DownloadStatus.Canceled;
            pending.Message = "Download canceled.";
            _ = PersistAsync();
        }
    }

    private async Task ProcessLoopAsync()
    {
        while (true)
        {
            await queueSignal.WaitAsync();
            var settings = await settingsService.LoadAsync();
            var maxParallel = Math.Max(1, settings.MaxParallelDownloads);

            while (currentRunningCount < maxParallel)
            {
                var job = Jobs.FirstOrDefault(x => x.Status == DownloadStatus.Pending);
                if (job is null)
                {
                    break;
                }

                Interlocked.Increment(ref currentRunningCount);
                _ = Task.Run(async () =>
                {
                    await RunJobAsync(job);
                    Interlocked.Decrement(ref currentRunningCount);
                    await PersistAsync();
                    queueSignal.Release();
                });
            }
        }
    }

    private async Task RunJobAsync(DownloadJob job)
    {
        var cts = new CancellationTokenSource();
        runningJobs[job.Id] = cts;

        try
        {
            job.Status = DownloadStatus.Running;
            var settings = await settingsService.LoadAsync();
            var progress = new Progress<ProgressUpdate>(update =>
            {
                if (update.Percent is not null)
                {
                    job.ProgressPercent = update.Percent.Value;
                }

                if (!string.IsNullOrWhiteSpace(update.Speed))
                {
                    job.SpeedText = update.Speed;
                }

                if (!string.IsNullOrWhiteSpace(update.Eta))
                {
                    job.EtaText = update.Eta;
                }
            });

            Exception? lastException = null;
            var attemptCount = Math.Max(1, settings.Retries + 1);
            for (var attempt = 1; attempt <= attemptCount; attempt++)
            {
                try
                {
                    await ytDlpService.DownloadAsync(job, settings, progress, cts.Token);
                    lastException = null;
                    break;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    job.Message = $"Attempt {attempt}/{attemptCount} failed: {ex.Message}";
                    await Task.Delay(1000, cts.Token);
                }
            }

            if (lastException is not null)
            {
                throw lastException;
            }

            job.Status = DownloadStatus.Completed;
            job.Message = "Download completed.";

            await historyService.AddAsync(new DownloadHistoryItem
            {
                Url = job.Url,
                Title = string.IsNullOrWhiteSpace(job.Title) ? job.Url : job.Title,
                OutputPath = job.OutputDirectory,
                IsSuccess = true,
                Message = "Completed"
            });
        }
        catch (OperationCanceledException)
        {
            job.Status = DownloadStatus.Canceled;
            job.Message = "Download canceled.";
        }
        catch (Exception ex)
        {
            job.Status = DownloadStatus.Failed;
            job.Message = ex.Message;

            await historyService.AddAsync(new DownloadHistoryItem
            {
                Url = job.Url,
                Title = string.IsNullOrWhiteSpace(job.Title) ? job.Url : job.Title,
                OutputPath = job.OutputDirectory,
                IsSuccess = false,
                Message = ex.Message
            });
        }
        finally
        {
            runningJobs.TryRemove(job.Id, out _);
        }
    }
}
