using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

public interface ISettingsService
{
    Task<AppSettings> LoadAsync();
    Task SaveAsync(AppSettings settings);
}
