namespace YtDlpGui.App.Services;

public interface IUserPromptService
{
    DuplicatePromptAction ResolveDuplicate(string url);
}
