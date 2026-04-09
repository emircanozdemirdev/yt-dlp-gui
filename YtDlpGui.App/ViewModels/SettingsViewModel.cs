using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpGui.App.Infrastructure;
using YtDlpGui.App.Models;
using YtDlpGui.App.Services;

namespace YtDlpGui.App.ViewModels;

public partial class SettingsViewModel(ISettingsService settingsService) : ObservableObject
{
    [ObservableProperty]
    private AppSettings current = new();

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string maxParallelDownloadsInput = "2";

    [ObservableProperty]
    private string retriesInput = "3";

    /// <summary>Shows output path with "Users\user" instead of the real account name (two-way binding).</summary>
    public string OutputDirectoryDisplay
    {
        get => UserProfilePathDisplay.MaskForDisplay(Current.OutputDirectory);
        set
        {
            var resolved = UserProfilePathDisplay.UnmaskFromDisplay(value?.Trim() ?? string.Empty);
            if (string.Equals(Current.OutputDirectory, resolved, StringComparison.Ordinal))
            {
                return;
            }

            Current.OutputDirectory = resolved;
            OnPropertyChanged();
        }
    }

    partial void OnCurrentChanged(AppSettings? oldValue, AppSettings newValue)
    {
        OnPropertyChanged(nameof(OutputDirectoryDisplay));
    }

    public async Task LoadAsync()
    {
        Current = await settingsService.LoadAsync();
        MaxParallelDownloadsInput = Current.MaxParallelDownloads.ToString(CultureInfo.InvariantCulture);
        RetriesInput = Current.Retries.ToString(CultureInfo.InvariantCulture);
        OnPropertyChanged(nameof(OutputDirectoryDisplay));
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!int.TryParse(
                MaxParallelDownloadsInput.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var maxParallel) ||
            maxParallel < 1)
        {
            StatusMessage = "Max parallel downloads must be a positive integer.";
            return;
        }

        if (!int.TryParse(
                RetriesInput.Trim(),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var retries) ||
            retries < 0)
        {
            StatusMessage = "Retries must be zero or a positive integer.";
            return;
        }

        Current.MaxParallelDownloads = maxParallel;
        Current.Retries = retries;
        await settingsService.SaveAsync(Current);
        StatusMessage = "Settings saved.";
    }
}
