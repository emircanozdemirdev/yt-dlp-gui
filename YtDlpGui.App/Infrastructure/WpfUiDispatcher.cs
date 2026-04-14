using System.Windows;

namespace YtDlpGui.App.Infrastructure;

public sealed class WpfUiDispatcher : IUiDispatcher
{
    public void Invoke(Action action)
    {
        if (Application.Current?.Dispatcher is null)
        {
            action();
            return;
        }

        Application.Current.Dispatcher.Invoke(action);
    }
}
