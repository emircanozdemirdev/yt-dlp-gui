using System.Collections.ObjectModel;
using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

public interface IQueueService
{
    ObservableCollection<DownloadJob> Jobs { get; }
    Task LoadPersistedJobsAsync();
    Task PersistAsync();
    Task EnqueueAsync(DownloadJob job);
    void Pause(Guid id);
    void Resume(Guid id);
    void RetryFailed();
    void Cancel(Guid id);
}
