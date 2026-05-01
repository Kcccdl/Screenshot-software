using System.IO;
using System.Text.Json;

namespace ScreenCapture.Models;

/// <summary>
/// 应用程序设置，支持持久化到 JSON 文件
/// </summary>
public class AppSettings
{
    /// <summary>设置文件路径（存放在应用程序目录下）</summary>
    private static readonly string SettingsPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    /// <summary>截图快捷键的虚拟键码（默认 PrintScreen = 0x2C）</summary>
    public uint HotkeyVk { get; set; } = 0x2C;

    /// <summary>快捷键修饰键（0=无, 1=Alt, 2=Ctrl, 4=Shift, 可组合）</summary>
    public uint HotkeyModifiers { get; set; } = 0;

    /// <summary>是否开机自启动</summary>
    public bool AutoStart { get; set; } = false;

    /// <summary>默认保存路径（空字符串表示每次弹出对话框）</summary>
    public string DefaultSavePath { get; set; } = string.Empty;

    /// <summary>默认保存格式</summary>
    public string DefaultSaveFormat { get; set; } = "Png";

    /// <summary>
    /// 从文件加载设置，如果文件不存在则返回默认设置
    /// </summary>
    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { /* 加载失败时使用默认设置 */ }
        return new AppSettings();
    }

    /// <summary>
    /// 保存设置到文件
    /// </summary>
    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch { /* 保存失败时静默忽略 */ }
    }

    /// <summary>
    /// 获取快捷键的显示文本（用于 UI 展示）
    /// </summary>
    public string GetHotkeyDisplayText()
    {
        var parts = new List<string>();
        if ((HotkeyModifiers & 0x02) != 0) parts.Add("Ctrl");
        if ((HotkeyModifiers & 0x01) != 0) parts.Add("Alt");
        if ((HotkeyModifiers & 0x04) != 0) parts.Add("Shift");
        parts.Add(GetKeyName(HotkeyVk));
        return string.Join(" + ", parts);
    }

    /// <summary>
    /// 将虚拟键码转换为可读的键名
    /// </summary>
    private static string GetKeyName(uint vk) => vk switch
    {
        0x2C => "PrintScreen",
        0x70 => "F1", 0x71 => "F2", 0x72 => "F3", 0x73 => "F4",
        0x74 => "F5", 0x75 => "F6", 0x76 => "F7", 0x77 => "F8",
        0x78 => "F9", 0x79 => "F10", 0x7A => "F11", 0x7B => "F12",
        0x1B => "Esc",
        0x20 => "Space",
        0x0D => "Enter",
        0x09 => "Tab",
        0x24 => "Home", 0x23 => "End",
        0x21 => "PageUp", 0x22 => "PageDown",
        0x25 => "←", 0x26 => "↑", 0x27 => "→", 0x28 => "↓",
        0x2D => "Insert", 0x2E => "Delete",
        0x5A => "Z", 0x58 => "X", 0x43 => "C", 0x56 => "V",
        0x41 => "A", 0x42 => "B", 0x44 => "D", 0x45 => "E",
        0x46 => "F", 0x47 => "G", 0x48 => "H", 0x49 => "I",
        0x4A => "J", 0x4B => "K", 0x4C => "L", 0x4D => "M",
        0x4E => "N", 0x4F => "O", 0x50 => "P", 0x51 => "Q",
        0x52 => "R", 0x53 => "S", 0x54 => "T", 0x55 => "U",
        0x57 => "W", 0x59 => "Y",
        _ => $"Key(0x{vk:X2})"
    };
}
