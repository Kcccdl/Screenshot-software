using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ScreenCapture.Services;

namespace ScreenCapture.Windows;

/// <summary>
/// 选区覆盖层窗口
/// 全屏窗口，截图作为背景，用户拖拽选择截图区域
/// </summary>
public partial class SelectionOverlayWindow : Window
{
    private readonly WindowDetector _windowDetector = new();
    private readonly BitmapSource _fullScreenBitmap;

    private bool _isSelecting;
    private Point _selectionStart;
    private Rect _selectionRect;
    private Rect? _highlightWindowRect;

    private double _screenW;
    private double _screenH;

    public event Action<Rect>? SelectionConfirmed;

    public SelectionOverlayWindow(BitmapSource fullScreenBitmap)
    {
        InitializeComponent();
        _fullScreenBitmap = fullScreenBitmap;
        // 立即记录屏幕尺寸，不依赖 ActualWidth
        _screenW = SystemParameters.PrimaryScreenWidth;
        _screenH = SystemParameters.PrimaryScreenHeight;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            BackgroundImage.Source = _fullScreenBitmap;

            // 用屏幕尺寸初始化遮罩（不依赖 ActualWidth，此时可能为 0）
            ShowFullDim();

            Focus();
            Keyboard.Focus(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"初始化失败: {ex.Message}");
            Close();
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
        else if (e.Key == Key.Enter && _selectionRect.Width > 5 && _selectionRect.Height > 5)
            ConfirmSelection();
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        Close();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(this);

        // 单击永远是开始拖拽选区（不自动确认窗口，避免无法拖拽）
        _isSelecting = true;
        _selectionStart = pos;
        _selectionRect = new Rect(pos, new Size(0, 0));

        // 隐藏窗口高亮，开始拖拽
        WindowHighlight.Visibility = Visibility.Collapsed;
        WindowTitleBorder.Visibility = Visibility.Collapsed;
        _highlightWindowRect = null;

        CaptureMouse();
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(this);

        // 十字准线
        CrosshairH.X1 = 0; CrosshairH.Y1 = pos.Y;
        CrosshairH.X2 = _screenW; CrosshairH.Y2 = pos.Y;
        CrosshairV.X1 = pos.X; CrosshairV.Y1 = 0;
        CrosshairV.X2 = pos.X; CrosshairV.Y2 = _screenH;
        CrosshairH.Visibility = Visibility.Visible;
        CrosshairV.Visibility = Visibility.Visible;

        if (_isSelecting)
        {
            double x = Math.Min(_selectionStart.X, pos.X);
            double y = Math.Min(_selectionStart.Y, pos.Y);
            _selectionRect = new Rect(x, y,
                Math.Abs(pos.X - _selectionStart.X),
                Math.Abs(pos.Y - _selectionStart.Y));
            UpdateSelectionVisuals();
            UpdateDimRects(_selectionRect);
        }
        else
        {
            DetectWindowUnderCursor();
        }
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting) return;
        _isSelecting = false;
        ReleaseMouseCapture();

        // 选区太小视为无效点击
        if (_selectionRect.Width < 5 || _selectionRect.Height < 5)
        {
            _selectionRect = Rect.Empty;
            ShowFullDim();
            SelectionBorder.Visibility = Visibility.Collapsed;
            DimensionBorder.Visibility = Visibility.Collapsed;
            return;
        }

        // 松开鼠标后自动确认选区
        ConfirmSelection();
    }

    private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_highlightWindowRect.HasValue)
            _selectionRect = _highlightWindowRect.Value;
        if (_selectionRect.Width > 5 && _selectionRect.Height > 5)
            ConfirmSelection();
    }

    /// <summary>显示全屏半透明遮罩</summary>
    private void ShowFullDim()
    {
        DimTop.Visibility = Visibility.Visible;
        DimTop.Width = _screenW;
        DimTop.Height = _screenH;
        Canvas.SetLeft(DimTop, 0);
        Canvas.SetTop(DimTop, 0);

        DimLeft.Visibility = Visibility.Collapsed;
        DimRight.Visibility = Visibility.Collapsed;
        DimBottom.Visibility = Visibility.Collapsed;
    }

    /// <summary>更新选区边框</summary>
    private void UpdateSelectionVisuals()
    {
        if (_selectionRect.Width < 1 || _selectionRect.Height < 1)
        {
            SelectionBorder.Visibility = Visibility.Collapsed;
            DimensionBorder.Visibility = Visibility.Collapsed;
            return;
        }

        SelectionBorder.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionBorder, _selectionRect.X);
        Canvas.SetTop(SelectionBorder, _selectionRect.Y);
        SelectionBorder.Width = _selectionRect.Width;
        SelectionBorder.Height = _selectionRect.Height;

        DimensionText.Text = $"{(int)_selectionRect.Width} x {(int)_selectionRect.Height}";
        DimensionBorder.Visibility = Visibility.Visible;
        Canvas.SetLeft(DimensionBorder, _selectionRect.X + _selectionRect.Width + 5);
        Canvas.SetTop(DimensionBorder, _selectionRect.Y);
    }

    /// <summary>更新选区外部的遮罩</summary>
    private void UpdateDimRects(Rect sel)
    {
        if (sel.Width < 1 || sel.Height < 1) { ShowFullDim(); return; }

        // 上
        Canvas.SetLeft(DimTop, 0); Canvas.SetTop(DimTop, 0);
        DimTop.Width = _screenW; DimTop.Height = sel.Y;
        DimTop.Visibility = Visibility.Visible;

        // 左
        Canvas.SetLeft(DimLeft, 0); Canvas.SetTop(DimLeft, sel.Y);
        DimLeft.Width = sel.X; DimLeft.Height = sel.Height;
        DimLeft.Visibility = Visibility.Visible;

        // 右
        Canvas.SetLeft(DimRight, sel.X + sel.Width); Canvas.SetTop(DimRight, sel.Y);
        DimRight.Width = _screenW - sel.X - sel.Width; DimRight.Height = sel.Height;
        DimRight.Visibility = Visibility.Visible;

        // 下
        Canvas.SetLeft(DimBottom, 0); Canvas.SetTop(DimBottom, sel.Y + sel.Height);
        DimBottom.Width = _screenW; DimBottom.Height = _screenH - sel.Y - sel.Height;
        DimBottom.Visibility = Visibility.Visible;
    }

    /// <summary>智能窗口检测</summary>
    private void DetectWindowUnderCursor()
    {
        try
        {
            var win = _windowDetector.GetWindowUnderCursor();
            if (win != null && win.Bounds.Width > 10 && win.Bounds.Height > 10)
            {
                _highlightWindowRect = win.Bounds;
                WindowHighlight.Visibility = Visibility.Visible;
                Canvas.SetLeft(WindowHighlight, win.Bounds.X);
                Canvas.SetTop(WindowHighlight, win.Bounds.Y);
                WindowHighlight.Width = win.Bounds.Width;
                WindowHighlight.Height = win.Bounds.Height;

                WindowTitleText.Text = string.IsNullOrEmpty(win.Title) ? "无标题窗口" : win.Title;
                WindowTitleBorder.Visibility = Visibility.Visible;
                Canvas.SetLeft(WindowTitleBorder, win.Bounds.X);
                Canvas.SetTop(WindowTitleBorder, Math.Max(0, win.Bounds.Y - 30));
            }
            else
            {
                _highlightWindowRect = null;
                WindowHighlight.Visibility = Visibility.Collapsed;
                WindowTitleBorder.Visibility = Visibility.Collapsed;
            }
        }
        catch
        {
            _highlightWindowRect = null;
            WindowHighlight.Visibility = Visibility.Collapsed;
            WindowTitleBorder.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>确认选区，转换为像素坐标</summary>
    private void ConfirmSelection()
    {
        try
        {
            double scaleX = _fullScreenBitmap.PixelWidth / _screenW;
            double scaleY = _fullScreenBitmap.PixelHeight / _screenH;

            int x = Math.Max(0, (int)(_selectionRect.X * scaleX));
            int y = Math.Max(0, (int)(_selectionRect.Y * scaleY));
            int w = (int)(_selectionRect.Width * scaleX);
            int h = (int)(_selectionRect.Height * scaleY);

            w = Math.Min(w, _fullScreenBitmap.PixelWidth - x);
            h = Math.Min(h, _fullScreenBitmap.PixelHeight - y);

            if (w > 0 && h > 0)
                SelectionConfirmed?.Invoke(new Rect(x, y, w, h));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"选区失败: {ex.Message}");
        }
        Close();
    }
}
