namespace YtDlpGui.App.Infrastructure;

public interface IProcessRunner
{
    Task<int> RunAsync(
        string fileName,
        string arguments,
        Action<string>? onOutput,
        Action<string>? onError,
        CancellationToken cancellationToken);
}
