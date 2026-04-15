using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Text.Json;
using YtDlpGui.App.Infrastructure;
using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

public sealed class QueueService(
    IYtDlpService ytDlpService,
    ISettingsService settingsService,
    IHistoryService historyService,
    IUiDispatcher uiDispatcher,
    IUserPromptService userPromptService,
    INotificationService notificationService) : IQueueService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    public ObservableCollection<DownloadJob> Jobs { get; } = [];

    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> runningJobs = new();
    private readonly ConcurrentDictionary<Guid, byte> pauseRequestedJobs = new();
    private readonly SemaphoreSlim queueSignal = new(0);
    private readonly SemaphoreSlim persistLock = new(1, 1);
    private readonly string queuePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "YtDlpGui",
        "queue.json");
    private bool isProcessorStarted;
    private int currentRunningCount;
    private volatile bool deleteTempFilesOnCancel = true;

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

        uiDispatcher.Invoke(() =>
        {
            foreach (var job in persisted)
            {
                if (job.Status is DownloadStatus.Pending or DownloadStatus.Running)
                {
                    job.Status = DownloadStatus.Pending;
                    job.ResumePartialDownload = true;
                    job.SpeedText = "-";
                    job.EtaText = "-";
                    job.Message = "Restored from previous session.";
                    Jobs.Add(job);
                }
                else if (job.Status == DownloadStatus.Paused)
                {
                    job.ResumePartialDownload = true;
                    job.SpeedText = "-";
                    job.EtaText = "-";
                    job.Message = "Paused from previous session.";
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
            uiDispatcher.Invoke(() =>
            {
                foreach (var job in Jobs)
                {
                    if (job.Status is DownloadStatus.Pending or DownloadStatus.Running or DownloadStatus.Paused)
                    {
                        snapshot.Add(job);
                    }
                }
            });

            if (snapshot.Count == 0)
            {
                if (File.Exists(queuePath))
                {
                    File.Delete(queuePath);
                }
                return;
            }

            await using var stream = File.Create(queuePath);
            await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions);
        }
        finally
        {
            persistLock.Release();
        }
    }

    public async Task<EnqueueResult> EnqueueAsync(DownloadJob job)
    {
        var settings = await settingsService.LoadAsync();
        var duplicateAction = await ResolveDuplicateActionAsync(job.Url, settings);
        if (duplicateAction == DuplicatePromptAction.Skip)
        {
            return new EnqueueResult
            {
                Accepted = false,
                Message = "Skipped because URL already exists."
            };
        }

        if (duplicateAction == DuplicatePromptAction.Replace)
        {
            RemoveExistingDuplicates(job.Url);
        }

        uiDispatcher.Invoke(() => Jobs.Add(job));
        await PersistAsync();
        queueSignal.Release();

        if (!isProcessorStarted)
        {
            isProcessorStarted = true;
            _ = Task.Run(ProcessLoopAsync);
        }

        return new EnqueueResult
        {
            Accepted = true,
            Message = "Added to queue."
        };
    }

    public void Cancel(Guid id)
    {
        pauseRequestedJobs.TryRemove(id, out _);

        if (runningJobs.TryGetValue(id, out var cts))
        {
            cts.Cancel();
            return;
        }

        var queued = Jobs.FirstOrDefault(x =>
            x.Id == id &&
            (x.Status == DownloadStatus.Pending || x.Status == DownloadStatus.Paused));
        if (queued is not null)
        {
            MarkCanceledAndLog(queued);
            if (deleteTempFilesOnCancel)
            {
                _ = Task.Run(() => CleanupCanceledArtifacts(queued));
            }
            uiDispatcher.Invoke(() => Jobs.Remove(queued));
            _ = PersistAsync();
        }
    }

    public void Pause(Guid id)
    {
        if (runningJobs.TryGetValue(id, out var cts))
        {
            pauseRequestedJobs[id] = 1;
            cts.Cancel();
            return;
        }

        var pending = Jobs.FirstOrDefault(x => x.Id == id && x.Status == DownloadStatus.Pending);
        if (pending is not null)
        {
            pending.Status = DownloadStatus.Paused;
            pending.Message = "Download paused.";
            _ = PersistAsync();
        }
    }

    public void Resume(Guid id)
    {
        pauseRequestedJobs.TryRemove(id, out _);

        var paused = Jobs.FirstOrDefault(x => x.Id == id && x.Status == DownloadStatus.Paused);
        if (paused is null)
        {
            return;
        }

        paused.Status = DownloadStatus.Pending;
        paused.ResumePartialDownload = true;
        paused.Message = "Resumed and queued.";
        _ = PersistAsync();
        queueSignal.Release();
    }

    public void RetryFailed()
    {
        var hasRetriedAny = false;
        foreach (var job in Jobs)
        {
            if (job.Status != DownloadStatus.Failed)
            {
                continue;
            }

            hasRetriedAny = true;
            job.Status = DownloadStatus.Pending;
            job.ResumePartialDownload = false;
            job.ProgressPercent = 0;
            job.SpeedText = "-";
            job.EtaText = "-";
            job.Message = "Retry queued.";
        }

        if (!hasRetriedAny)
        {
            return;
        }

        _ = PersistAsync();
        queueSignal.Release();
    }

    public void Retry(Guid id)
    {
        var job = Jobs.FirstOrDefault(x => x.Id == id);
        if (job is null)
        {
            return;
        }

        if (job.Status is DownloadStatus.Running or DownloadStatus.Pending)
        {
            return;
        }

        job.Status = DownloadStatus.Pending;
        job.ResumePartialDownload = false;
        job.ProgressPercent = 0;
        job.SpeedText = "-";
        job.EtaText = "-";
        job.Message = "Retry queued.";
        _ = PersistAsync();
        queueSignal.Release();
    }

    public void Remove(Guid id)
    {
        var job = Jobs.FirstOrDefault(x => x.Id == id);
        if (job is null)
        {
            return;
        }

        if (job.Status == DownloadStatus.Running)
        {
            Cancel(id);
        }

        uiDispatcher.Invoke(() => Jobs.Remove(job));
        _ = PersistAsync();
    }

    private async Task ProcessLoopAsync()
    {
        while (true)
        {
            await queueSignal.WaitAsync();
            var settings = await settingsService.LoadAsync();
            deleteTempFilesOnCancel = settings.DeleteTempFilesOnCancel;
            var maxParallel = Math.Max(1, settings.MaxParallelDownloads);

            while (currentRunningCount < maxParallel)
            {
                var job = Jobs.FirstOrDefault(x => x.Status == DownloadStatus.Pending);
                if (job is null)
                {
                    break;
                }

                if (!BandwidthWindowEvaluator.IsWithinWindow(settings, DateTime.Now))
                {
                    job.Message = "Waiting for allowed time window.";
                    _ = Task.Delay(TimeSpan.FromSeconds(30))
                        .ContinueWith(_ => queueSignal.Release(), TaskScheduler.Default);
                    break;
                }

                job.EffectiveRateLimitKbps = BandwidthWindowEvaluator.ResolveEffectiveRateLimit(settings);

                var cts = new CancellationTokenSource();
                runningJobs[job.Id] = cts;
                Interlocked.Increment(ref currentRunningCount);
                _ = Task.Run(async () =>
                {
                    await RunJobAsync(job, cts);
                    Interlocked.Decrement(ref currentRunningCount);
                    await PersistAsync();
                    queueSignal.Release();
                });
            }
        }
    }

    private async Task RunJobAsync(DownloadJob job, CancellationTokenSource cts)
    {
        AppSettings? currentSettings = null;
        try
        {
            job.Status = DownloadStatus.Running;
            if (string.IsNullOrWhiteSpace(job.TemporaryDirectory))
            {
                job.TemporaryDirectory = Path.Combine(
                    Path.GetTempPath(),
                    "YtDlpGui",
                    "downloads",
                    job.Id.ToString("N"));
            }
            Directory.CreateDirectory(job.TemporaryDirectory);
            currentSettings = await settingsService.LoadAsync();
            deleteTempFilesOnCancel = currentSettings.DeleteTempFilesOnCancel;
            var maxObservedPercent = Math.Clamp(job.ProgressPercent, 0, 100);
            var lastUiUpdateAt = DateTime.UtcNow;
            var progress = new Progress<ProgressUpdate>(update =>
            {
                if (cts.IsCancellationRequested || job.Status != DownloadStatus.Running)
                {
                    return;
                }

                var now = DateTime.UtcNow;
                var shouldUpdateUi = now - lastUiUpdateAt >= TimeSpan.FromMilliseconds(200);

                if (update.Percent is not null)
                {
                    if (job.IsPlaylist && job.PlaylistItemCount > 0 && job.CurrentPlaylistIndex > 0)
                    {
                        var nextItemProgress = Math.Clamp(update.Percent.Value, 0, 100);
                        if (Math.Abs(nextItemProgress - job.CurrentItemProgressPercent) >= 0.2 || shouldUpdateUi)
                        {
                            job.CurrentItemProgressPercent = nextItemProgress;
                        }

                        var completedBeforeCurrent = Math.Max(0, job.CurrentPlaylistIndex - 1);
                        var overallProgress = ((completedBeforeCurrent + (job.CurrentItemProgressPercent / 100.0)) / job.PlaylistItemCount) * 100.0;
                        maxObservedPercent = Math.Max(maxObservedPercent, overallProgress);
                    }
                    else
                    {
                        maxObservedPercent = Math.Max(maxObservedPercent, update.Percent.Value);
                    }

                    if (Math.Abs(maxObservedPercent - job.ProgressPercent) >= 0.1 || shouldUpdateUi)
                    {
                        job.ProgressPercent = maxObservedPercent;
                    }
                }

                if (!string.IsNullOrWhiteSpace(update.Speed) && (shouldUpdateUi || update.Speed != job.SpeedText))
                {
                    job.SpeedText = update.Speed;
                }

                if (!string.IsNullOrWhiteSpace(update.Eta) && (shouldUpdateUi || update.Eta != job.EtaText))
                {
                    job.EtaText = update.Eta;
                }

                if (shouldUpdateUi)
                {
                    lastUiUpdateAt = now;
                }
            });

            Exception? lastException = null;
            var attemptCount = Math.Max(1, currentSettings.Retries + 1);
            for (var attempt = 1; attempt <= attemptCount; attempt++)
            {
                try
                {
                    await ytDlpService.DownloadAsync(job, currentSettings, progress, cts.Token);
                    lastException = null;
                    break;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (cts.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(cts.Token);
                    }

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
            job.ResumePartialDownload = false;
            job.CurrentItemProgressPercent = 100;
            if (job.IsPlaylist && job.PlaylistItemCount > 0)
            {
                job.CurrentPlaylistIndex = job.PlaylistItemCount;
            }
            job.ProgressPercent = 100;
            job.Message = "Download completed.";

            await historyService.AddAsync(new DownloadHistoryItem
            {
                Url = job.Url,
                Title = string.IsNullOrWhiteSpace(job.Title) ? job.Url : job.Title,
                OutputPath = job.OutputDirectory,
                IsSuccess = true,
                Message = "Completed"
            });
            await NotifyCompletionAsync(currentSettings ?? new AppSettings(), job, isSuccess: true);
        }
        catch (OperationCanceledException)
        {
            if (pauseRequestedJobs.TryRemove(job.Id, out _))
            {
                job.Status = DownloadStatus.Paused;
                job.ResumePartialDownload = true;
                job.Message = "Download paused.";
                job.SpeedText = "-";
                job.EtaText = "-";
            }
            else
            {
                MarkCanceledAndLog(job);
                if (deleteTempFilesOnCancel)
                {
                    CleanupCanceledArtifacts(job);
                }
                uiDispatcher.Invoke(() => Jobs.Remove(job));
            }
        }
        catch (Exception ex)
        {
            job.Status = DownloadStatus.Failed;
            job.ResumePartialDownload = false;
            job.Message = ex.Message;

            await historyService.AddAsync(new DownloadHistoryItem
            {
                Url = job.Url,
                Title = string.IsNullOrWhiteSpace(job.Title) ? job.Url : job.Title,
                OutputPath = job.OutputDirectory,
                IsSuccess = false,
                Message = ex.Message
            });
            await NotifyCompletionAsync(currentSettings ?? new AppSettings(), job, isSuccess: false);
        }
        finally
        {
            runningJobs.TryRemove(job.Id, out _);
            pauseRequestedJobs.TryRemove(job.Id, out _);
        }
    }

    private static void CleanupCanceledArtifacts(DownloadJob job)
    {
        if (!string.IsNullOrWhiteSpace(job.TemporaryDirectory) && Directory.Exists(job.TemporaryDirectory))
        {
            try
            {
                Directory.Delete(job.TemporaryDirectory, recursive: true);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }

        var artifacts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in job.GetArtifactPathsSnapshot())
        {
            artifacts.Add(path);
        }

        if (!string.IsNullOrWhiteSpace(job.OutputFilePath))
        {
            artifacts.Add(job.OutputFilePath.Trim());
        }

        if (artifacts.Count == 0)
        {
            return;
        }

        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var artifact in artifacts)
        {
            candidates.Add(artifact);
            candidates.Add($"{artifact}.part");
            candidates.Add($"{artifact}.ytdl");

            var directory = Path.GetDirectoryName(artifact);
            var fileName = Path.GetFileName(artifact);
            if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(fileName) || !Directory.Exists(directory))
            {
                continue;
            }

            var searchPattern = $"{fileName}*";
            foreach (var path in Directory.EnumerateFiles(directory, searchPattern))
            {
                if (path.EndsWith(".part", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase))
                {
                    candidates.Add(path);
                }
            }
        }

        foreach (var path in candidates)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }

    private void MarkCanceledAndLog(DownloadJob job)
    {
        job.Status = DownloadStatus.Canceled;
        job.ResumePartialDownload = false;
        job.ProgressPercent = 0;
        job.Message = "Download canceled.";
        job.SpeedText = "-";
        job.EtaText = "-";
        _ = historyService.AddAsync(new DownloadHistoryItem
        {
            Url = job.Url,
            Title = string.IsNullOrWhiteSpace(job.Title) ? job.Url : job.Title,
            OutputPath = job.OutputDirectory,
            IsSuccess = false,
            Message = "Canceled"
        });
        _ = NotifyCanceledAsync(job);
    }

    private async Task NotifyCanceledAsync(DownloadJob job)
    {
        var settings = await settingsService.LoadAsync();
        await NotifyCompletionAsync(settings, job, isSuccess: false);
    }

    private async Task<DuplicatePromptAction> ResolveDuplicateActionAsync(string url, AppSettings settings)
    {
        if (settings.DuplicatePolicy == DuplicatePolicy.Allow)
        {
            return DuplicatePromptAction.AddAnyway;
        }

        var hasQueueDuplicate = Jobs.Any(x => string.Equals(x.Url, url, StringComparison.OrdinalIgnoreCase));
        var history = await historyService.LoadAsync();
        var hasHistoryDuplicate = history.Any(x => string.Equals(x.Url, url, StringComparison.OrdinalIgnoreCase));
        var isDuplicate = hasQueueDuplicate || hasHistoryDuplicate;
        if (!isDuplicate)
        {
            return DuplicatePromptAction.AddAnyway;
        }

        return settings.DuplicatePolicy switch
        {
            DuplicatePolicy.Skip => DuplicatePromptAction.Skip,
            DuplicatePolicy.Replace => DuplicatePromptAction.Replace,
            DuplicatePolicy.Ask => userPromptService.ResolveDuplicate(url),
            _ => DuplicatePromptAction.AddAnyway
        };
    }

    private void RemoveExistingDuplicates(string url)
    {
        var duplicates = Jobs
            .Where(x =>
                string.Equals(x.Url, url, StringComparison.OrdinalIgnoreCase) &&
                x.Status is DownloadStatus.Pending or DownloadStatus.Paused)
            .ToList();

        foreach (var duplicate in duplicates)
        {
            uiDispatcher.Invoke(() => Jobs.Remove(duplicate));
        }
    }

    private Task NotifyCompletionAsync(AppSettings settings, DownloadJob job, bool isSuccess)
    {
        if (settings.NotifyOnCompletion)
        {
            var status = isSuccess ? "Completed" : "Failed";
            notificationService.Notify("yt-dlp GUI", $"{status}: {job.Title}");
        }

        if (settings.PlaySoundOnCompletion)
        {
            notificationService.PlayCompletionSound();
        }

        return Task.CompletedTask;
    }
}
