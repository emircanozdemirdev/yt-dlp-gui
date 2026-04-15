using System.Windows;

namespace YtDlpGui.App.Services;

public sealed class DesktopUserPromptService : IUserPromptService
{
    public DuplicatePromptAction ResolveDuplicate(string url)
    {
        var result = MessageBox.Show(
            $"This URL already exists in queue/history.\n\n{url}\n\nYes = Replace existing\nNo = Add anyway\nCancel = Skip",
            "Duplicate URL",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        return result switch
        {
            MessageBoxResult.Yes => DuplicatePromptAction.Replace,
            MessageBoxResult.No => DuplicatePromptAction.AddAnyway,
            _ => DuplicatePromptAction.Skip
        };
    }
}
