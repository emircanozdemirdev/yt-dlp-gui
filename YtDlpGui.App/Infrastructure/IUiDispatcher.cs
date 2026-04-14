namespace YtDlpGui.App.Infrastructure;

public interface IUiDispatcher
{
    void Invoke(Action action);
}
