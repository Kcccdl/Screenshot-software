using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace ScreenCapture.Services;

/// <summary>
/// 全局热键管理器
/// 使用 Win32 RegisterHotKey API 注册系统级热键
/// 通过隐藏的 HwndSource 窗口接收 WM_HOTKEY 消息
/// </summary>
public class HotkeyManager : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;  // 热键消息 ID

    private HwndSource? _hwndSource;        // 消息接收窗口
    private IntPtr _hwnd;                   // 窗口句柄
    private readonly Dictionary<int, Action> _hotkeyActions = new(); // 热键 ID -> 回调
    private int _currentId;                 // 热键 ID 计数器

    /// <summary>热键按下事件</summary>
    public event EventHandler<int>? HotkeyPressed;

    /// <summary>
    /// 初始化热键管理器，创建隐藏的消息窗口
    /// </summary>
    public void Initialize()
    {
        var parameters = new HwndSourceParameters("ScreenCaptureHotkeyReceiver")
        {
            Width = 0, Height = 0,
            WindowStyle = 0 // 不可见
        };
        _hwndSource = new HwndSource(parameters);
        _hwnd = _hwndSource.Handle;
        _hwndSource.AddHook(WndProc);
    }

    /// <summary>
    /// 注册全局热键
    /// </summary>
    /// <param name="modifiers">修饰键（0=无, 1=Alt, 2=Ctrl, 4=Shift）</param>
    /// <param name="vk">虚拟键码</param>
    /// <param name="callback">热键按下时的回调函数</param>
    /// <returns>热键 ID，用于后续取消注册</returns>
    public int RegisterHotkey(uint modifiers, uint vk, Action callback)
    {
        int id = ++_currentId;
        if (!RegisterHotKey(_hwnd, id, modifiers, vk))
            throw new InvalidOperationException($"注册热键失败 id={id}");
        _hotkeyActions[id] = callback;
        return id;
    }

    /// <summary>
    /// 取消注册全局热键
    /// </summary>
    public void UnregisterHotkey(int id)
    {
        UnregisterHotKey(_hwnd, id);
        _hotkeyActions.Remove(id);
    }

    /// <summary>
    /// 窗口消息处理回调，处理 WM_HOTKEY 消息
    /// </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                action?.Invoke();
                HotkeyPressed?.Invoke(this, id);
            }
            handled = true;
        }
        return IntPtr.Zero;
    }

    /// <summary>释放所有热键和消息窗口</summary>
    public void Dispose()
    {
        foreach (var id in _hotkeyActions.Keys.ToList())
            UnregisterHotkey(id);
        _hwndSource?.Dispose();
    }
}
