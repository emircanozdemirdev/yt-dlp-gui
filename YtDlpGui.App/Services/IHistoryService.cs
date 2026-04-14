using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

public interface IHistoryService
{
    Task<IReadOnlyList<DownloadHistoryItem>> LoadAsync();
    Task AddAsync(DownloadHistoryItem item);
    Task DeleteAsync(IEnumerable<Guid> ids);
}
