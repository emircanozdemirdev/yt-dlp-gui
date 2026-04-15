namespace YtDlpGui.App.Services;

public interface INotificationService
{
    void Notify(string title, string message);
    void PlayCompletionSound();
}
