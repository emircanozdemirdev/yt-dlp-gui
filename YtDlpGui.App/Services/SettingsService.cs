using System.Text.Json;
using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string settingsPath;

    public SettingsService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "YtDlpGui");
        Directory.CreateDirectory(appData);
        settingsPath = Path.Combine(appData, "settings.json");
    }

    public async Task<AppSettings> LoadAsync()
    {
        await ToolBootstrapper.EnsureToolsPresentAsync();

        AppSettings settings;
        if (!File.Exists(settingsPath))
        {
            settings = new AppSettings();
        }
        else
        {
            var json = await File.ReadAllTextAsync(settingsPath);
            settings = AppSettingsJson.DeserializeSafe(json);
        }

        var pathsChanged = ToolPathResolver.ApplyToolPaths(settings);
        if (!File.Exists(settingsPath) || pathsChanged)
        {
            await SaveAsync(settings);
        }

        return settings;
    }

    public async Task SaveAsync(AppSettings settings)
    {
        await using var stream = File.Create(settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions);
    }
}
