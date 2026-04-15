using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpGui.App.Infrastructure;
using YtDlpGui.App.Models;
using YtDlpGui.App.Services;

namespace YtDlpGui.App.ViewModels;

public partial class SettingsViewModel(
    ISettingsService settingsService,
    IThemeService themeService) : ObservableObject
{
    [ObservableProperty]
    private AppSettings current = new();

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string maxParallelDownloadsInput = "2";

    [ObservableProperty]
    private string retriesInput = "3";

    [ObservableProperty]
    private AppTheme selectedTheme = AppTheme.Dark;

    [ObservableProperty]
    private string bandwidthLimitKbpsInput = string.Empty;

    [ObservableProperty]
    private string bandwidthWindowStartInput = "23:00";

    [ObservableProperty]
    private string bandwidthWindowEndInput = "07:00";

    [ObservableProperty]
    private DuplicatePolicy selectedDuplicatePolicy = DuplicatePolicy.Allow;

    public IReadOnlyList<AppTheme> ThemeOptions { get; } =
        [AppTheme.Dark, AppTheme.Light];
    public IReadOnlyList<DuplicatePolicy> DuplicatePolicyOptions { get; } =
        [DuplicatePolicy.Allow, DuplicatePolicy.Skip, DuplicatePolicy.Ask, DuplicatePolicy.Replace];

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
        BandwidthLimitKbpsInput = Current.BandwidthLimitKbps?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        BandwidthWindowStartInput = Current.BandwidthWindowStart;
        BandwidthWindowEndInput = Current.BandwidthWindowEnd;
        SelectedDuplicatePolicy = Current.DuplicatePolicy;
        SelectedTheme = Current.Theme;
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
        Current.BandwidthLimitKbps = ParseNullablePositiveInt(BandwidthLimitKbpsInput);
        Current.BandwidthWindowStart = string.IsNullOrWhiteSpace(BandwidthWindowStartInput) ? "23:00" : BandwidthWindowStartInput.Trim();
        Current.BandwidthWindowEnd = string.IsNullOrWhiteSpace(BandwidthWindowEndInput) ? "07:00" : BandwidthWindowEndInput.Trim();
        Current.DuplicatePolicy = SelectedDuplicatePolicy;
        Current.Theme = SelectedTheme;
        themeService.Apply(SelectedTheme);
        await settingsService.SaveAsync(Current);
        StatusMessage = "Settings saved.";
    }

    private static int? ParseNullablePositiveInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : null;
    }
}
