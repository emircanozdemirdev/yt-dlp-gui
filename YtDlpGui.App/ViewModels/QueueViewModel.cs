using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpGui.App.Models;
using YtDlpGui.App.Services;

namespace YtDlpGui.App.ViewModels;

public partial class QueueViewModel : ObservableObject
{
    private readonly IQueueService queueService;
    [ObservableProperty]
    private int selectedCount;

    public QueueViewModel(IQueueService queueService)
    {
        this.queueService = queueService;
        Jobs = queueService.Jobs;
        Jobs.CollectionChanged += OnJobsCollectionChanged;
        foreach (var job in Jobs)
        {
            job.PropertyChanged += OnJobPropertyChanged;
        }
        RecalculateSelectionState();
    }

    public ObservableCollection<DownloadJob> Jobs { get; }
    public bool HasSelection => SelectedCount > 0;
    public string SelectionSummary => $"{SelectedCount} selected / {Jobs.Count} total";

    partial void OnSelectedCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectionSummary));
    }

    [RelayCommand]
    private void Cancel(DownloadJob? job)
    {
        if (job is null)
        {
            return;
        }

        queueService.Cancel(job.Id);
    }

    [RelayCommand]
    private void PauseResume(DownloadJob? job)
    {
        if (job is null)
        {
            return;
        }

        if (job.Status is DownloadStatus.Pending or DownloadStatus.Running)
        {
            queueService.Pause(job.Id);
            return;
        }

        if (job.Status == DownloadStatus.Paused)
        {
            queueService.Resume(job.Id);
        }
    }

    [RelayCommand]
    private void RetryFailed()
    {
        queueService.RetryFailed();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var job in Jobs)
        {
            job.IsSelected = true;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var job in Jobs)
        {
            job.IsSelected = false;
        }
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        var selectedIds = Jobs.Where(x => x.IsSelected).Select(x => x.Id).ToList();
        foreach (var id in selectedIds)
        {
            queueService.Remove(id);
        }
        RecalculateSelectionState();
    }

    [RelayCommand]
    private void PauseResumeSelected()
    {
        var selected = Jobs.Where(x => x.IsSelected).ToList();
        // #region agent log
        WriteDebugLog(
            "pre-fix-1",
            "H1",
            "QueueViewModel.cs:PauseResumeSelected",
            "PauseResumeSelected invoked",
            new
            {
                selectedCount = selected.Count,
                selected = selected.Select(x => new { x.Id, status = x.Status.ToString(), x.ProgressPercent }).ToList()
            });
        // #endregion
        foreach (var job in selected)
        {
            PauseResume(job);
        }
    }

    [RelayCommand]
    private void CancelSelected()
    {
        var selected = Jobs.Where(x => x.IsSelected).ToList();
        foreach (var job in selected)
        {
            Cancel(job);
        }
    }

    [RelayCommand]
    private void RedownloadSelected()
    {
        var selectedIds = Jobs.Where(x => x.IsSelected).Select(x => x.Id).ToList();
        foreach (var id in selectedIds)
        {
            queueService.Retry(id);
        }
        RecalculateSelectionState();
    }

    private void OnJobsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (DownloadJob item in e.OldItems)
            {
                item.PropertyChanged -= OnJobPropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (DownloadJob item in e.NewItems)
            {
                item.PropertyChanged += OnJobPropertyChanged;
            }
        }

        RecalculateSelectionState();
    }

    private void OnJobPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DownloadJob.IsSelected))
        {
            RecalculateSelectionState();
        }
    }

    private void RecalculateSelectionState()
    {
        SelectedCount = Jobs.Count(x => x.IsSelected);
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectionSummary));
    }

    private static void WriteDebugLog(
        string runId,
        string hypothesisId,
        string location,
        string message,
        object data)
    {
        try
        {
            var payload = new
            {
                sessionId = "0b91bd",
                runId,
                hypothesisId,
                location,
                message,
                data,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            File.AppendAllText("debug-0b91bd.log", JsonSerializer.Serialize(payload) + Environment.NewLine);
        }
        catch
        {
            // Best-effort debug logging.
        }
    }
}
