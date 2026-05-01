using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ScreenCapture.Models;
using ScreenCapture.Utils;
using Application = System.Windows.Application;

namespace ScreenCapture.Windows;

/// <summary>
/// 主窗口
/// 截图工具的入口界面，提供截图按钮、快捷键显示和设置入口
/// 负责注册全局热键并触发截图流程
/// </summary>
public partial class MainWindow : Window
{
    #region Win32 API

    private const int WM_HOTKEY = 0x0312;  // 热键消息 ID

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    #endregion

    private const int HOTKEY_ID = 9000;    // 热键注册 ID
    private HwndSource? _hwndSource;        // 消息钩子窗口
    private AppSettings _settings;          // 应用设置

    public MainWindow()
    {
        InitializeComponent();
        _settings = AppSettings.Load();
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    /// <summary>窗口加载时设置图标和注册热键</summary>
    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 设置应用图标
        try
        {
            var iconPath = IconGenerator.GenerateIcon();
            if (File.Exists(iconPath))
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(iconPath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                Icon = bmp;
            }
        }
        catch { }

        var helper = new WindowInteropHelper(this);
        _hwndSource = HwndSource.FromHwnd(helper.Handle);
        _hwndSource?.AddHook(WndProc);

        RegisterCurrentHotkey();
        HotkeyDisplayText.Text = _settings.GetHotkeyDisplayText();
    }

    /// <summary>窗口关闭时注销热键</summary>
    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        UnregisterCurrentHotkey();
        _hwndSource?.RemoveHook(WndProc);
    }

    /// <summary>注册当前设置的全局热键</summary>
    private void RegisterCurrentHotkey()
    {
        var helper = new WindowInteropHelper(this);
        try
        {
            RegisterHotKey(helper.Handle, HOTKEY_ID, _settings.HotkeyModifiers, _settings.HotkeyVk);
        }
        catch { /* 热键注册失败时静默忽略 */ }
    }

    /// <summary>注销当前全局热键</summary>
    private void UnregisterCurrentHotkey()
    {
        var helper = new WindowInteropHelper(this);
        try { UnregisterHotKey(helper.Handle, HOTKEY_ID); } catch { }
    }

    /// <summary>窗口消息回调：收到热键消息时触发截图</summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            StartScreenshot();
            handled = true;
        }
        return IntPtr.Zero;
    }

    /// <summary>点击"开始截图"按钮</summary>
    private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
    {
        StartScreenshot();
    }

    /// <summary>
    /// 开始截图流程
    /// 最小化主窗口 -> 延迟 -> 截图 -> 打开覆盖层
    /// </summary>
    private void StartScreenshot()
    {
        // 最小化主窗口，避免截到自己
        WindowState = WindowState.Minimized;

        // 延迟 300ms 等窗口完全最小化后再截图
        Dispatcher.InvokeAsync(async () =>
        {
            await System.Threading.Tasks.Task.Delay(300);
            try
            {
                if (Application.Current is App app)
                {
                    app.StartScreenshot(RestoreMainWindow);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"截图失败: {ex.Message}", "错误");
                RestoreMainWindow();
            }
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    /// <summary>
    /// 恢复主窗口显示
    /// 截图完成或取消后调用
    /// </summary>
    public void RestoreMainWindow()
    {
        Dispatcher.Invoke(() =>
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        });
    }

    /// <summary>打开设置窗口</summary>
    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow(_settings);
        settingsWindow.SettingsSaved += (newSettings) =>
        {
            UnregisterCurrentHotkey();
            _settings = newSettings;
            RegisterCurrentHotkey();
            HotkeyDisplayText.Text = _settings.GetHotkeyDisplayText();
        };
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }
}
