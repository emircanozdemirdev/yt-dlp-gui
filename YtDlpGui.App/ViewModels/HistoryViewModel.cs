using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpGui.App.Models;
using YtDlpGui.App.Services;

namespace YtDlpGui.App.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IHistoryService historyService;
    private readonly IQueueService queueService;

    [ObservableProperty]
    private int selectedCount;

    public HistoryViewModel(IHistoryService historyService, IQueueService queueService)
    {
        this.historyService = historyService;
        this.queueService = queueService;
    }

    public ObservableCollection<DownloadHistoryItem> Items { get; } = [];
    public bool HasSelection => SelectedCount > 0;
    public bool CanOpenSelectedFolder => Items.Any(x => x.IsSelected && IsFolderOpenable(x.OutputPath));
    public string SelectionSummary => $"{SelectedCount} selected / {Items.Count} total";

    public async Task LoadAsync()
    {
        foreach (var item in Items)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
        }

        Items.Clear();
        var items = await historyService.LoadAsync();
        foreach (var item in items)
        {
            item.PropertyChanged += OnItemPropertyChanged;
            Items.Add(item);
        }
        RecalculateSelectionState();
    }

    partial void OnSelectedCountChanged(int value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(CanOpenSelectedFolder));
        OnPropertyChanged(nameof(SelectionSummary));
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in Items)
        {
            item.IsSelected = true;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        foreach (var item in Items)
        {
            item.IsSelected = false;
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedAsync()
    {
        var selected = Items.Where(x => x.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        await historyService.DeleteAsync(selected.Select(x => x.Id));
        foreach (var item in selected)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
            Items.Remove(item);
        }
        RecalculateSelectionState();
    }

    [RelayCommand]
    private async Task RedownloadSelectedAsync()
    {
        var selected = Items.Where(x => x.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        foreach (var item in selected)
        {
            var outputDirectory = string.IsNullOrWhiteSpace(item.OutputPath)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Downloads")
                : item.OutputPath;

            var job = new DownloadJob
            {
                Url = item.Url,
                Title = item.Title,
                OutputDirectory = outputDirectory,
                SelectedFormatId = "best"
            };

            var enqueueResult = await queueService.EnqueueAsync(job);
            if (!enqueueResult.Accepted)
            {
                continue;
            }
        }

        await historyService.DeleteAsync(selected.Select(x => x.Id));
        foreach (var item in selected)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
            Items.Remove(item);
        }

        RecalculateSelectionState();
    }

    [RelayCommand]
    private void OpenSelectedFolder()
    {
        var selected = Items.FirstOrDefault(x => x.IsSelected);
        if (selected is null)
        {
            return;
        }

        var folderPath = ResolveFolderPath(selected.OutputPath);
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = folderPath,
            UseShellExecute = true
        });
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DownloadHistoryItem.IsSelected))
        {
            RecalculateSelectionState();
        }
    }

    private void RecalculateSelectionState()
    {
        SelectedCount = Items.Count(x => x.IsSelected);
        OnPropertyChanged(nameof(CanOpenSelectedFolder));
    }

    private static string? ResolveFolderPath(string? outputPath)
    {
        var path = outputPath?.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Directory.Exists(path) ? path : Path.GetDirectoryName(path);
    }

    private static bool IsFolderOpenable(string? outputPath)
    {
        var folderPath = ResolveFolderPath(outputPath);
        return !string.IsNullOrWhiteSpace(folderPath) && Directory.Exists(folderPath);
    }
}
