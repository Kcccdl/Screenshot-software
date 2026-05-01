namespace ScreenCapture.Models.Enums;

/// <summary>
/// 截图模式枚举
/// </summary>
public enum CaptureMode
{
    /// <summary>全屏截图</summary>
    FullScreen,
    /// <summary>区域截图（手动拖拽选区）</summary>
    Region,
    /// <summary>窗口截图（智能检测窗口）</summary>
    Window
}
