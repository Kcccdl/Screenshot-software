using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using ScreenCapture.Models;

namespace ScreenCapture.Windows;

/// <summary>
/// 设置窗口
/// 用于自定义截图快捷键
/// 支持 Ctrl/Alt/Shift + 任意键的组合
/// </summary>
public partial class SettingsWindow : Window
{
    [DllImport("user32.dll")]
    private static extern int GetKeyNameText(int lParam, System.Text.StringBuilder lpString, int nMaxSize);

    private readonly AppSettings _settings;     // 当前设置（副本）
    private uint _newModifiers;                  // 新的修饰键
    private uint _newVk;                         // 新的虚拟键码
    private bool _waitingForKey;                 // 是否正在等待按键

    /// <summary>设置保存事件</summary>
    public event Action<AppSettings>? SettingsSaved;

    public SettingsWindow(AppSettings currentSettings)
    {
        InitializeComponent();
        _settings = currentSettings;
        _newModifiers = currentSettings.HotkeyModifiers;
        _newVk = currentSettings.HotkeyVk;
        Loaded += SettingsWindow_Loaded;
    }

    private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CurrentHotkeyText.Text = _settings.GetHotkeyDisplayText();
        _waitingForKey = true;
        NewHotkeyText.Text = "请按下快捷键...";

        // 窗口获得焦点以接收键盘事件
        Focus();
        KeyDown += OnKeyDown;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    /// <summary>
    /// 捕获键盘按下事件，记录修饰键和主键
    /// </summary>
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_waitingForKey) return;

        // 拦截所有按键，不让系统处理
        e.Handled = true;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (!_waitingForKey) return;
        e.Handled = true;

        // 获取修饰键状态
        uint modifiers = 0;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) modifiers |= 0x02;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)) modifiers |= 0x01;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) modifiers |= 0x04;

        // 获取主键（忽略单独的修饰键按下）
        Key key = e.Key;
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin ||
            key == Key.System)
            return;

        // 如果没有修饰键，不允许设置（避免冲突）
        if (modifiers == 0 && key != Key.PrintScreen)
        {
            NewHotkeyText.Text = "请包含 Ctrl/Alt/Shift 修饰键";
            NewHotkeyText.Foreground = System.Windows.Media.Brushes.Orange;
            return;
        }

        // 转换 WPF Key 为虚拟键码
        _newVk = (uint)KeyInterop.VirtualKeyFromKey(key);
        _newModifiers = modifiers;

        // 显示新的快捷键文本
        var parts = new System.Collections.Generic.List<string>();
        if ((modifiers & 0x02) != 0) parts.Add("Ctrl");
        if ((modifiers & 0x01) != 0) parts.Add("Alt");
        if ((modifiers & 0x04) != 0) parts.Add("Shift");
        parts.Add(key.ToString());

        NewHotkeyText.Text = string.Join(" + ", parts);
        NewHotkeyText.Foreground = System.Windows.Media.Brushes.LimeGreen;
        _waitingForKey = false;
    }

    /// <summary>恢复默认快捷键（PrintScreen）</summary>
    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _newModifiers = 0;
        _newVk = 0x2C; // PrintScreen
        NewHotkeyText.Text = "PrintScreen";
        NewHotkeyText.Foreground = System.Windows.Media.Brushes.LimeGreen;
        _waitingForKey = false;
    }

    /// <summary>保存设置并关闭窗口</summary>
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.HotkeyModifiers = _newModifiers;
        _settings.HotkeyVk = _newVk;
        _settings.Save();
        SettingsSaved?.Invoke(_settings);
        DialogResult = true;
        Close();
    }

    /// <summary>取消并关闭窗口</summary>
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
