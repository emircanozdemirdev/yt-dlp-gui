using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YtDlpGui.App.Models;
using YtDlpGui.App.Services;

namespace YtDlpGui.App.ViewModels;

public partial class HistoryViewModel(IHistoryService historyService) : ObservableObject
{
    public ObservableCollection<DownloadHistoryItem> Items { get; } = [];

    public async Task LoadAsync()
    {
        Items.Clear();
        var items = await historyService.LoadAsync();
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }
}
