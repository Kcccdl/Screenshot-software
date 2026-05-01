using System.Runtime.InteropServices;
using System.Windows;

namespace ScreenCapture.Services;

/// <summary>
/// 窗口信息数据类
/// </summary>
public class WindowInfo
{
    /// <summary>窗口句柄</summary>
    public IntPtr Handle { get; set; }
    /// <summary>窗口标题</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>窗口在屏幕上的矩形区域</summary>
    public Rect Bounds { get; set; }
}

/// <summary>
/// 窗口检测器
/// 使用 Win32 API 检测鼠标下方的窗口，用于智能窗口选择功能
/// </summary>
public class WindowDetector
{
    #region Win32 API 声明

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
        public int Width => Right - Left;
        public int Height => Bottom - Top;
        public Rect ToRect() => new(Left, Top, Width, Height);
    }

    private const int DWMWA_EXTENDED_FRAME_BOUNDS = 9; // DWM 扩展窗口边界属性

    #endregion

    /// <summary>
    /// 获取鼠标光标下方的顶层窗口
    /// 通过 WindowFromPoint 获取命中的窗口，然后沿父窗口链向上查找顶层窗口
    /// </summary>
    public WindowInfo? GetWindowUnderCursor()
    {
        GetCursorPos(out POINT pt);
        IntPtr hWnd = WindowFromPoint(pt);
        if (hWnd == IntPtr.Zero) return null;

        // 沿父窗口链向上查找顶层窗口
        while (hWnd != IntPtr.Zero)
        {
            IntPtr parent = GetParent(hWnd);
            if (parent == IntPtr.Zero || parent == GetDesktopWindow())
                break;
            hWnd = parent;
        }

        if (hWnd == IntPtr.Zero || !IsWindowVisible(hWnd))
            return null;

        return BuildWindowInfo(hWnd);
    }

    /// <summary>
    /// 枚举所有可见的顶层窗口（调试用）
    /// </summary>
    public List<WindowInfo> EnumerateVisibleWindows()
    {
        var windows = new List<WindowInfo>();
        EnumWindows((hWnd, _) =>
        {
            if (IsWindowVisible(hWnd))
                windows.Add(BuildWindowInfo(hWnd));
            return true; // 继续枚举
        }, IntPtr.Zero);
        return windows;
    }

    /// <summary>
    /// 构建窗口信息对象
    /// 优先使用 DWM 扩展边界（更准确），失败时回退到 GetWindowRect
    /// </summary>
    private static WindowInfo BuildWindowInfo(IntPtr hWnd)
    {
        Rect bounds;
        if (DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out RECT dwmRect, Marshal.SizeOf<RECT>()) == 0)
            bounds = dwmRect.ToRect();
        else
        {
            GetWindowRect(hWnd, out RECT rect);
            bounds = rect.ToRect();
        }

        int len = GetWindowTextLength(hWnd);
        var title = new System.Text.StringBuilder(len + 1);
        GetWindowText(hWnd, title, title.Capacity);

        return new WindowInfo
        {
            Handle = hWnd,
            Title = title.ToString().Trim(),
            Bounds = bounds
        };
    }
}
