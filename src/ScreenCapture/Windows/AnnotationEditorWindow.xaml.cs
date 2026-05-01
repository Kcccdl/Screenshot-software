using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenCapture.Models.Enums;
using ScreenCapture.ViewModels;

namespace ScreenCapture.Windows;

/// <summary>
/// 标注编辑器窗口
/// </summary>
public partial class AnnotationEditorWindow : Window
{
    private readonly AnnotationEditorViewModel _viewModel;
    private readonly BitmapSource _screenshotBitmap;
    private readonly List<ToggleButton> _toolButtons = new();

    // 双击检测
    private DateTime _lastClickTime = DateTime.MinValue;
    private Point _lastClickPos;
    private const int DoubleClickMs = 400;

    // 文字编辑框引用
    private TextBox? _activeTextBox;
    private bool _textEditingConfirmed; // 标记文字编辑是否已确认

    public AnnotationEditorWindow(BitmapSource screenshotBitmap)
    {
        InitializeComponent();
        _screenshotBitmap = screenshotBitmap;
        _viewModel = (AnnotationEditorViewModel)DataContext;
        Loaded += Window_Loaded;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var bgImage = new Image
        {
            Source = _screenshotBitmap,
            Width = _screenshotBitmap.PixelWidth,
            Height = _screenshotBitmap.PixelHeight
        };
        AnnotationCanvas.Children.Add(bgImage);
        AnnotationCanvas.Width = _screenshotBitmap.PixelWidth;
        AnnotationCanvas.Height = _screenshotBitmap.PixelHeight;

        _viewModel.BackgroundImage = _screenshotBitmap;
        _viewModel.SetCanvas(AnnotationCanvas);
        _viewModel.RequestTextEdit = ShowTextEditor;

        FindToolButtons(this);
    }

    private void FindToolButtons(DependencyObject parent)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is ToggleButton tb && tb.Tag is ShapeType)
                _toolButtons.Add(tb);
            else
                FindToolButtons(child);
        }
    }

    /// <summary>
    /// 弹出文字编辑框
    /// Enter 确认 | 右键取消 | 点击编辑框外部确认
    /// </summary>
    /// <param name="position">编辑框位置</param>
    /// <param name="initialText">初始文字（二次编辑时为原有文字，新建时为空）</param>
    /// <param name="onConfirm">确认回调</param>
    private void ShowTextEditor(Point position, string initialText, Action<string> onConfirm)
    {
        var textBox = new TextBox
        {
            Text = initialText ?? "",
            FontSize = _viewModel.TextFontSize,
            FontFamily = new FontFamily(_viewModel.TextFontFamily),
            FontWeight = _viewModel.TextIsBold ? FontWeights.Bold : FontWeights.Normal,
            FontStyle = _viewModel.TextIsItalic ? FontStyles.Italic : FontStyles.Normal,
            Foreground = new SolidColorBrush(_viewModel.StrokeColor),
            Background = new SolidColorBrush(Color.FromArgb(230, 255, 255, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
            BorderThickness = new Thickness(2),
            Padding = new Thickness(4, 2, 4, 2),
            MinWidth = 80,
            MinHeight = 24,
            AcceptsReturn = false
        };

        // 同步 UI 控件（二次编辑时回填字体设置）
        SyncFontControlsFromViewModel();

        Canvas.SetLeft(textBox, position.X);
        Canvas.SetTop(textBox, position.Y);
        AnnotationCanvas.Children.Add(textBox);
        _activeTextBox = textBox;
        _textEditingConfirmed = false;

        textBox.Focus();
        // 新建时全选，二次编辑时光标移到末尾
        if (string.IsNullOrEmpty(initialText))
            textBox.SelectAll();
        else
            textBox.CaretIndex = textBox.Text.Length;

        void Confirm()
        {
            if (_textEditingConfirmed) return;
            _textEditingConfirmed = true;
            AnnotationCanvas.Children.Remove(textBox);
            _activeTextBox = null;
            onConfirm(textBox.Text);
        }
        void Cancel()
        {
            if (_textEditingConfirmed) return;
            _textEditingConfirmed = true;
            AnnotationCanvas.Children.Remove(textBox);
            _activeTextBox = null;
            // 不调用 onConfirm，保持原文字不变
        }

        // Enter 确认
        textBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter) { Confirm(); e.Handled = true; }
        };
        // 右键取消
        textBox.PreviewMouseRightButtonDown += (s, e) =>
        {
            Cancel();
            e.Handled = true;
        };
        // 点击编辑框外部 → 确认编辑
        MouseButtonEventHandler? canvasHandler = null;
        canvasHandler = (s, e) =>
        {
            if (_textEditingConfirmed) return;
            var clickPos = e.GetPosition(AnnotationCanvas);
            double left = Canvas.GetLeft(textBox);
            double top = Canvas.GetTop(textBox);
            var textBoxRect = new Rect(left - 30, top - 30,
                textBox.ActualWidth + 60, textBox.ActualHeight + 60);
            if (!textBoxRect.Contains(clickPos))
                Confirm();
        };
        AnnotationCanvas.MouseDown += canvasHandler;
        textBox.Unloaded += (s, e) =>
        {
            AnnotationCanvas.MouseDown -= canvasHandler;
        };
        // 不在 LostFocus 时确认，避免点击工具栏控件导致退出编辑
        // 确认方式：Enter 键 或 点击画布空白区域
        // 取消方式：右键
    }

    /// <summary>
    /// 将 ViewModel 的文字设置同步到 UI 控件（字体、字号、粗体等）
    /// 二次编辑时调用，让 UI 显示该文字的实际设置
    /// </summary>
    private void SyncFontControlsFromViewModel()
    {
        // 字体
        for (int i = 0; i < FontFamilyCombo.Items.Count; i++)
        {
            if (FontFamilyCombo.Items[i] is ComboBoxItem item &&
                item.Tag?.ToString() == _viewModel.TextFontFamily)
            {
                FontFamilyCombo.SelectedIndex = i;
                break;
            }
        }
        // 字号
        for (int i = 0; i < FontSizeCombo.Items.Count; i++)
        {
            if (FontSizeCombo.Items[i] is ComboBoxItem item &&
                double.TryParse(item.Tag?.ToString(), out double size) &&
                Math.Abs(size - _viewModel.TextFontSize) < 0.1)
            {
                FontSizeCombo.SelectedIndex = i;
                break;
            }
        }
        // 粗体、斜体、下划线、删除线
        BoldToggle.IsChecked = _viewModel.TextIsBold;
        ItalicToggle.IsChecked = _viewModel.TextIsItalic;
        UnderlineToggle.IsChecked = _viewModel.TextIsUnderline;
        StrikethroughToggle.IsChecked = _viewModel.TextIsStrikethrough;
    }

    // ========== 工具栏事件 ==========

    private void ToolButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton btn && btn.Tag is ShapeType type)
        {
            _viewModel.ActiveTool = type;
            foreach (var tb in _toolButtons)
                if (tb != btn) tb.IsChecked = false;
            btn.IsChecked = true;
        }
    }

    private void ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string colorStr)
            _viewModel.StrokeColor = (Color)ColorConverter.ConvertFromString(colorStr);
    }

    private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_viewModel == null) return;
        _viewModel.StrokeThickness = e.NewValue;
        ThicknessLabel.Text = ((int)e.NewValue).ToString();
    }

    // ========== 文字设置事件 ==========

    private void FontFamily_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel == null || FontFamilyCombo.SelectedItem is not ComboBoxItem item) return;
        _viewModel.TextFontFamily = item.Tag?.ToString() ?? "Microsoft YaHei";
    }

    private void FontSize_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel == null || FontSizeCombo.SelectedItem is not ComboBoxItem item) return;
        if (double.TryParse(item.Tag?.ToString(), out double size))
            _viewModel.TextFontSize = size;
    }

    private void Bold_Changed(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        _viewModel.TextIsBold = BoldToggle.IsChecked == true;
    }

    private void Italic_Changed(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        _viewModel.TextIsItalic = ItalicToggle.IsChecked == true;
    }

    private void Underline_Changed(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        _viewModel.TextIsUnderline = UnderlineToggle.IsChecked == true;
    }

    private void Strikethrough_Changed(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null) return;
        _viewModel.TextIsStrikethrough = StrikethroughToggle.IsChecked == true;
    }

    // ========== 画布鼠标事件 ==========

    private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // 如果有文字编辑框，画布点击由 canvasHandler 处理确认
        // 这里不做额外处理，避免与 canvasHandler 冲突
        if (_activeTextBox != null) return;

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(AnnotationCanvas);

            var now = DateTime.Now;
            bool isDoubleClick = (now - _lastClickTime).TotalMilliseconds < DoubleClickMs
                && (pos - _lastClickPos).Length < 10;
            _lastClickTime = now;
            _lastClickPos = pos;

            if (isDoubleClick)
            {
                _viewModel.OnDoubleClick(pos);
                return;
            }

            _viewModel.OnMouseDown(pos);
            AnnotationCanvas.CaptureMouse();
        }
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        _viewModel.OnMouseMove(e.GetPosition(AnnotationCanvas));
    }

    private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _viewModel.OnMouseUp(e.GetPosition(AnnotationCanvas));
        AnnotationCanvas.ReleaseMouseCapture();
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        if (_activeTextBox != null) return;
        _viewModel.ClearSelection();
        base.OnMouseRightButtonDown(e);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            _viewModel.DeleteSelectedCommand.Execute(null);
            return;
        }
        if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
            _viewModel.UndoCommand.Execute(null);
        else if (e.Key == Key.Y && Keyboard.Modifiers == ModifierKeys.Control)
            _viewModel.RedoCommand.Execute(null);
        else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
            _viewModel.CopyToClipboardCommand.Execute(null);
        else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            _viewModel.SaveAsCommand.Execute(null);
        else if (e.Key == Key.Escape)
            Close();
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e) { }
}
