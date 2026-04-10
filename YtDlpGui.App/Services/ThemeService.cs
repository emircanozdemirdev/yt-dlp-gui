using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using YtDlpGui.App.Models;

namespace YtDlpGui.App.Services;

public sealed class ThemeService : IThemeService
{
    private const int DwmwaUseImmersiveDarkMode = 20;

    public void Apply(AppTheme theme)
    {
        var app = Application.Current;
        if (app?.Resources.MergedDictionaries is not { } merged)
        {
            return;
        }

        for (var i = merged.Count - 1; i >= 0; i--)
        {
            var src = merged[i].Source?.OriginalString ?? string.Empty;
            if (src.Contains("UiColorsLight.xaml", StringComparison.OrdinalIgnoreCase) ||
                src.Contains("UiColorsDark.xaml", StringComparison.OrdinalIgnoreCase))
            {
                merged.RemoveAt(i);
            }
        }

        var uri = theme == AppTheme.Dark ? "Themes/UiColorsDark.xaml" : "Themes/UiColorsLight.xaml";
        merged.Add(new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) });

        ApplyWindowChromeTheme(theme);
    }

    private static void ApplyWindowChromeTheme(AppTheme theme)
    {
        var useDarkMode = theme == AppTheme.Dark ? 1 : 0;
        foreach (Window window in Application.Current.Windows)
        {
            var interopHelper = new WindowInteropHelper(window);
            if (interopHelper.Handle == IntPtr.Zero)
            {
                continue;
            }

            _ = DwmSetWindowAttribute(
                interopHelper.Handle,
                DwmwaUseImmersiveDarkMode,
                ref useDarkMode,
                sizeof(int));
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);
}
