using Microsoft.Win32;
using System.Windows;

namespace ScreenCapture.Themes;

/// <summary>
/// 主题管理器
/// 检测系统明暗模式，动态切换应用主题
/// </summary>
public static class ThemeManager
{
    /// <summary>当前是否为暗色模式</summary>
    public static bool IsDarkMode { get; private set; }

    /// <summary>主题变化事件</summary>
    public static event Action<bool>? ThemeChanged;

    /// <summary>
    /// 初始化：检测系统主题并应用
    /// </summary>
    public static void Initialize()
    {
        IsDarkMode = GetSystemDarkMode();
        ApplyTheme(IsDarkMode);

        // 监听系统主题变化
        SystemEvents.UserPreferenceChanged += (s, e) =>
        {
            bool newMode = GetSystemDarkMode();
            if (newMode != IsDarkMode)
            {
                IsDarkMode = newMode;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ApplyTheme(IsDarkMode);
                    ThemeChanged?.Invoke(IsDarkMode);
                });
            }
        };
    }

    /// <summary>
    /// 读取注册表获取系统是否为暗色模式
    /// </summary>
    private static bool GetSystemDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int v && v == 0; // 0 = 暗色, 1 = 亮色
        }
        catch
        {
            return false; // 默认亮色
        }
    }

    /// <summary>
    /// 切换主题资源字典
    /// </summary>
    private static void ApplyTheme(bool dark)
    {
        var app = Application.Current;
        if (app == null) return;

        // 移除旧主题
        var existing = app.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source != null &&
                (d.Source.OriginalString.Contains("LightTheme.xaml") ||
                 d.Source.OriginalString.Contains("DarkTheme.xaml")));
        if (existing != null)
            app.Resources.MergedDictionaries.Remove(existing);

        // 添加新主题
        var themeUri = dark
            ? new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
            : new Uri("Themes/LightTheme.xaml", UriKind.Relative);

        app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
    }
}
