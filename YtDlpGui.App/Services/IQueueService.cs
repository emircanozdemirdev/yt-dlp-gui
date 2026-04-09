using System.Collections.ObjectModel;
using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

public interface IQueueService
{
    ObservableCollection<DownloadJob> Jobs { get; }
    Task EnqueueAsync(DownloadJob job);
    void Cancel(Guid id);
}
