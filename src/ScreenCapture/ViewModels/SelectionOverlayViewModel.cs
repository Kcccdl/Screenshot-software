using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenCapture.Services;

namespace ScreenCapture.ViewModels;

/// <summary>
/// 选区覆盖层的 ViewModel
/// 处理鼠标拖拽选区、智能窗口检测、选区确认/取消等逻辑
/// </summary>
public partial class SelectionOverlayViewModel : ViewModelBase
{
    private readonly WindowDetector _windowDetector = new();

    /// <summary>全屏截图位图（作为覆盖层背景）</summary>
    [ObservableProperty] private BitmapSource? _fullScreenBitmap;

    /// <summary>当前选区矩形</summary>
    [ObservableProperty] private Rect _selectionRect;

    /// <summary>智能检测到的窗口矩形（高亮显示）</summary>
    [ObservableProperty] private Rect? _highlightWindowRect;

    /// <summary>检测到的窗口标题</summary>
    [ObservableProperty] private string _windowTitle = string.Empty;

    /// <summary>是否正在拖拽选区</summary>
    [ObservableProperty] private bool _isSelecting;

    /// <summary>选区拖拽起始点</summary>
    [ObservableProperty] private Point _selectionStart;

    /// <summary>选区尺寸显示文本</summary>
    [ObservableProperty] private string _dimensionText = string.Empty;

    /// <summary>是否使用窗口检测模式</summary>
    [ObservableProperty] private bool _useWindowMode;

    /// <summary>选区确认事件（传递选区矩形）</summary>
    public event Action<Rect>? SelectionConfirmed;

    /// <summary>选区取消事件</summary>
    public event Action? SelectionCancelled;

    /// <summary>当选区矩形变化时更新尺寸显示文本</summary>
    partial void OnSelectionRectChanged(Rect value)
    {
        DimensionText = value.Width > 0 && value.Height > 0
            ? $"{(int)value.Width} x {(int)value.Height}"
            : string.Empty;
    }

    /// <summary>
    /// 鼠标移动处理
    /// 窗口模式下检测并高亮鼠标下方的窗口
    /// 拖拽模式下更新选区矩形
    /// </summary>
    [RelayCommand]
    public void OnMouseMove(Point position)
    {
        if (UseWindowMode)
        {
            var win = _windowDetector.GetWindowUnderCursor();
            if (win != null && win.Handle != IntPtr.Zero)
            {
                HighlightWindowRect = win.Bounds;
                WindowTitle = win.Title;
            }
            else
            {
                HighlightWindowRect = null;
                WindowTitle = string.Empty;
            }
        }

        if (IsSelecting)
        {
            double x = Math.Min(SelectionStart.X, position.X);
            double y = Math.Min(SelectionStart.Y, position.Y);
            SelectionRect = new Rect(x, y,
                Math.Abs(position.X - SelectionStart.X),
                Math.Abs(position.Y - SelectionStart.Y));
        }
    }

    /// <summary>鼠标按下处理：开始拖拽选区或确认窗口选择</summary>
    [RelayCommand]
    public void OnMouseDown(Point position)
    {
        if (UseWindowMode && HighlightWindowRect.HasValue)
        {
            SelectionRect = HighlightWindowRect.Value;
            SelectionConfirmed?.Invoke(SelectionRect);
            return;
        }
        IsSelecting = true;
        SelectionStart = position;
        SelectionRect = new Rect(position, new Size(0, 0));
    }

    /// <summary>鼠标释放处理：完成选区拖拽</summary>
    [RelayCommand]
    public void OnMouseUp(Point position)
    {
        if (!IsSelecting) return;
        IsSelecting = false;
        if (SelectionRect.Width > 5 && SelectionRect.Height > 5)
            SelectionConfirmed?.Invoke(SelectionRect);
    }

    /// <summary>双击处理：选中高亮的窗口</summary>
    [RelayCommand]
    public void OnDoubleClick(Point position)
    {
        if (UseWindowMode && HighlightWindowRect.HasValue)
            SelectionRect = HighlightWindowRect.Value;
        if (SelectionRect.Width > 5 && SelectionRect.Height > 5)
            SelectionConfirmed?.Invoke(SelectionRect);
    }

    /// <summary>取消选区</summary>
    [RelayCommand]
    public void Cancel()
    {
        SelectionCancelled?.Invoke();
    }
}
