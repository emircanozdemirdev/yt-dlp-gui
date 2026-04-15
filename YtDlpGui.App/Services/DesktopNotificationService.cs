using System.Media;
using System.Windows;

namespace YtDlpGui.App.Services;

public sealed class DesktopNotificationService : INotificationService
{
    public void Notify(string title, string message)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        });
    }

    public void PlayCompletionSound()
    {
        SystemSounds.Asterisk.Play();
    }
}
