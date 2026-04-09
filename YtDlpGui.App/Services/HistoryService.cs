using System.Text.Json;
using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

public sealed class HistoryService : IHistoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string historyPath;

    public HistoryService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YtDlpGui");
        Directory.CreateDirectory(appData);
        historyPath = Path.Combine(appData, "history.json");
    }

    public async Task<IReadOnlyList<DownloadHistoryItem>> LoadAsync()
    {
        if (!File.Exists(historyPath))
        {
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(historyPath);
            return await JsonSerializer.DeserializeAsync<List<DownloadHistoryItem>>(stream) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public async Task AddAsync(DownloadHistoryItem item)
    {
        var items = (await LoadAsync()).ToList();
        items.Insert(0, item);

        await using var stream = File.Create(historyPath);
        await JsonSerializer.SerializeAsync(stream, items, JsonOptions);
    }
}
