using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpGui.App.Models;
using YtDlpGui.App.Services;

namespace YtDlpGui.App.ViewModels;

public partial class HistoryViewModel(
    IHistoryService historyService,
    IQueueService queueService) : ObservableObject
{
    [ObservableProperty]
    private int selectedCount;

    public ObservableCollection<DownloadHistoryItem> Items { get; } = [];
    public bool HasSelection => SelectedCount > 0;
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

            await queueService.EnqueueAsync(job);
        }

        await historyService.DeleteAsync(selected.Select(x => x.Id));
        foreach (var item in selected)
        {
            item.PropertyChanged -= OnItemPropertyChanged;
            Items.Remove(item);
        }

        RecalculateSelectionState();
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
    }
}
