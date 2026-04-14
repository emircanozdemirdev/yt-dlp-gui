using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpGui.App.Models;
using YtDlpGui.App.Services;

namespace YtDlpGui.App.ViewModels;

public partial class QueueViewModel : ObservableObject
{
    private readonly IQueueService queueService;

    public QueueViewModel(IQueueService queueService)
    {
        this.queueService = queueService;
        Jobs = queueService.Jobs;
    }

    public ObservableCollection<DownloadJob> Jobs { get; }

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
}
