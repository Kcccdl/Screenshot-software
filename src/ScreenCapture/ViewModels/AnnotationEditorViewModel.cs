using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScreenCapture.Models.Commands;
using ScreenCapture.Models.Enums;
using ScreenCapture.Models.Shapes;
using ScreenCapture.Services;

namespace ScreenCapture.ViewModels;

/// <summary>
/// 标注编辑器的 ViewModel
/// 支持绘制标注、选中移动、编辑删除、撤销重做
/// </summary>
public partial class AnnotationEditorViewModel : ViewModelBase
{
    private readonly List<IAnnotationShape> _shapes = new();
    private readonly Stack<IAnnotationCommand> _undoStack = new();
    private readonly Stack<IAnnotationCommand> _redoStack = new();
    private int _sequenceCounter = 1;

    // 核心：追踪每个形状在画布上的实际 UIElement 实例
    private readonly Dictionary<IAnnotationShape, List<UIElement>> _shapeVisuals = new();

    [ObservableProperty] private BitmapSource? _backgroundImage;
    [ObservableProperty] private ShapeType _activeTool = ShapeType.Rectangle;
    [ObservableProperty] private Color _strokeColor = Colors.Red;
    [ObservableProperty] private double _strokeThickness = 2.0;
    [ObservableProperty] private int _mosaicBlockSize = 10;
    [ObservableProperty] private bool _canUndo;
    [ObservableProperty] private bool _canRedo;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty] private bool _hasSelection;

    // 文字标注设置
    [ObservableProperty] private string _textFontFamily = "Microsoft YaHei";
    [ObservableProperty] private double _textFontSize = 16.0;
    [ObservableProperty] private bool _textIsBold;
    [ObservableProperty] private bool _textIsItalic;
    [ObservableProperty] private bool _textIsUnderline;
    [ObservableProperty] private bool _textIsStrikethrough;

    private IAnnotationShape? _currentShape;
    private Canvas? _canvas;

    // 选中状态
    private IAnnotationShape? _selectedShape;
    private System.Windows.Shapes.Rectangle? _selectionRect;
    private bool _isDragging;
    private Point _dragOffset;

    /// <summary>文字编辑回调（位置、初始文字、确认回调）</summary>
    public Action<Point, string, Action<string>>? RequestTextEdit { get; set; }

    public void SetCanvas(Canvas canvas) => _canvas = canvas;

    #region 将形状添加到画布 / 从画布移除

    /// <summary>将形状的可视化元素添加到画布并记录</summary>
    private void AddShapeToCanvas(IAnnotationShape shape)
    {
        if (_canvas == null) return;
        var visuals = shape.CreateVisualElements().ToList();
        _shapeVisuals[shape] = visuals;
        foreach (var vis in visuals)
            _canvas.Children.Add(vis);
    }

    /// <summary>从画布移除形状的所有可视化元素</summary>
    private void RemoveShapeFromCanvas(IAnnotationShape shape)
    {
        if (_canvas == null) return;
        if (_shapeVisuals.TryGetValue(shape, out var visuals))
        {
            foreach (var vis in visuals)
                _canvas.Children.Remove(vis);
            _shapeVisuals.Remove(shape);
        }
    }

    #endregion

    #region 鼠标事件

    /// <summary>鼠标按下：先检查点中已有标注，否则开始绘制</summary>
    public void OnMouseDown(Point position)
    {
        if (_canvas == null) return;

        // 检查是否点中已有标注
        var hitShape = FindShapeAt(position);
        if (hitShape != null)
        {
            StartDrag(hitShape, position);
            return;
        }

        // 点空白：取消选中
        ClearSelection();

        // 文字工具
        if (ActiveTool == ShapeType.Text)
        {
            CreateTextAnnotation(position);
            return;
        }

        // 开始绘制新形状
        _currentShape = CreateShape(ActiveTool);
        if (_currentShape == null) return;

        _currentShape.StartPoint = position;
        _currentShape.EndPoint = position;
        _currentShape.StrokeColor = StrokeColor;
        _currentShape.StrokeThickness = StrokeThickness;

        if (_currentShape is MosaicShape mosaic)
        {
            mosaic.SourceBitmap = BackgroundImage;
            mosaic.BlockSize = MosaicBlockSize;
        }
        if (_currentShape is SequenceNumberShape seq)
            seq.SequenceNumber = _sequenceCounter++;
        if (_currentShape is FreeDrawShape freeDraw)
            freeDraw.AddPoint(position);

        // 添加到画布
        AddShapeToCanvas(_currentShape);
    }

    /// <summary>鼠标移动</summary>
    public void OnMouseMove(Point position)
    {
        // 拖拽选中的标注
        if (_isDragging && _selectedShape != null)
        {
            double dx = position.X - _dragOffset.X - _selectedShape.StartPoint.X;
            double dy = position.Y - _dragOffset.Y - _selectedShape.StartPoint.Y;
            _selectedShape.StartPoint = new Point(position.X - _dragOffset.X, position.Y - _dragOffset.Y);
            _selectedShape.EndPoint = new Point(_selectedShape.EndPoint.X + dx, _selectedShape.EndPoint.Y + dy);
            _selectedShape.UpdateVisualElements();
            UpdateSelectionRect();
            return;
        }

        if (_currentShape == null) return;
        if (_currentShape is FreeDrawShape freeDraw)
            freeDraw.AddPoint(position);
        _currentShape.EndPoint = position;
        _currentShape.UpdateVisualElements();
    }

    /// <summary>鼠标释放</summary>
    public void OnMouseUp(Point position)
    {
        if (_isDragging) { _isDragging = false; return; }
        if (_currentShape == null) return;

        if (_currentShape is FreeDrawShape freeDraw)
            freeDraw.AddPoint(position);
        else
            _currentShape.EndPoint = position;
        _currentShape.UpdateVisualElements();

        var dx = Math.Abs(_currentShape.EndPoint.X - _currentShape.StartPoint.X);
        var dy = Math.Abs(_currentShape.EndPoint.Y - _currentShape.StartPoint.Y);
        if (dx < 3 && dy < 3 && !(_currentShape is FreeDrawShape) && !(_currentShape is SequenceNumberShape))
        {
            // 太小，移除
            RemoveShapeFromCanvas(_currentShape);
        }
        else
        {
            _shapes.Add(_currentShape);
            _undoStack.Push(new AddShapeCommand(_shapes, _currentShape, _canvas!));
            _redoStack.Clear();
            UpdateUndoRedoState();
        }
        _currentShape = null;
    }

    /// <summary>双击：编辑文字</summary>
    public void OnDoubleClick(Point position)
    {
        var hit = FindShapeAt(position);
        if (hit is TextAnnotationShape textShape)
        {
            // 编辑时使用该文字原有的字体设置
            TextFontFamily = textShape.FontFamilyName;
            TextFontSize = textShape.FontSize;
            TextIsBold = textShape.IsBold;
            TextIsItalic = textShape.IsItalic;
            TextIsUnderline = textShape.IsUnderline;
            TextIsStrikethrough = textShape.IsStrikethrough;

            RequestTextEdit?.Invoke(textShape.StartPoint, textShape.Text, (newText) =>
            {
                if (!string.IsNullOrWhiteSpace(newText))
                {
                    textShape.Text = newText;
                    textShape.FontFamilyName = TextFontFamily;
                    textShape.FontSize = TextFontSize;
                    textShape.IsBold = TextIsBold;
                    textShape.IsItalic = TextIsItalic;
                    textShape.IsUnderline = TextIsUnderline;
                    textShape.IsStrikethrough = TextIsStrikethrough;
                    RemoveShapeFromCanvas(textShape);
                    AddShapeToCanvas(textShape);
                    if (_selectedShape == textShape)
                        UpdateSelectionRect();
                }
            });
        }
    }

    #endregion

    #region 选中、移动、删除

    private IAnnotationShape? FindShapeAt(Point position)
    {
        for (int i = _shapes.Count - 1; i >= 0; i--)
        {
            if (_shapes[i].HitTest(position))
                return _shapes[i];
        }
        return null;
    }

    private void StartDrag(IAnnotationShape shape, Point clickPos)
    {
        SelectShape(shape);
        _isDragging = true;
        _dragOffset = new Point(clickPos.X - shape.StartPoint.X, clickPos.Y - shape.StartPoint.Y);
    }

    private void SelectShape(IAnnotationShape shape)
    {
        ClearSelection();
        _selectedShape = shape;
        _selectedShape.IsSelected = true;
        HasSelection = true;

        _selectionRect = new System.Windows.Shapes.Rectangle
        {
            Stroke = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 2 },
            Fill = new SolidColorBrush(Color.FromArgb(20, 0, 120, 212)),
            IsHitTestVisible = false
        };
        _canvas!.Children.Add(_selectionRect);
        UpdateSelectionRect();
        StatusText = "选中标注 | Delete 删除 | 双击编辑文字 | 点击空白取消";
    }

    private void UpdateSelectionRect()
    {
        if (_selectionRect == null || _selectedShape == null) return;
        double x = Math.Min(_selectedShape.StartPoint.X, _selectedShape.EndPoint.X);
        double y = Math.Min(_selectedShape.StartPoint.Y, _selectedShape.EndPoint.Y);
        double w = Math.Abs(_selectedShape.EndPoint.X - _selectedShape.StartPoint.X);
        double h = Math.Abs(_selectedShape.EndPoint.Y - _selectedShape.StartPoint.Y);
        if (w < 20) w = 20;
        if (h < 15) h = 15;
        double pad = 6;
        Canvas.SetLeft(_selectionRect, x - pad);
        Canvas.SetTop(_selectionRect, y - pad);
        _selectionRect.Width = w + pad * 2;
        _selectionRect.Height = h + pad * 2;
    }

    public void ClearSelection()
    {
        if (_selectedShape != null)
        {
            _selectedShape.IsSelected = false;
            _selectedShape = null;
        }
        if (_selectionRect != null)
        {
            _canvas?.Children.Remove(_selectionRect);
            _selectionRect = null;
        }
        _isDragging = false;
        HasSelection = false;
        StatusText = string.Empty;
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (_selectedShape == null || _canvas == null) return;
        var shape = _selectedShape;
        ClearSelection();

        // 用追踪字典移除正确的 UIElement
        RemoveShapeFromCanvas(shape);
        _shapes.Remove(shape);
        StatusText = "已删除标注";
        UpdateUndoRedoState();
    }

    #endregion

    #region 撤销/重做/复制/保存/清除

    [RelayCommand]
    private void Undo()
    {
        ClearSelection();
        if (_undoStack.Count == 0) return;
        var cmd = _undoStack.Pop();
        cmd.Undo();
        _redoStack.Push(cmd);
        UpdateUndoRedoState();
    }

    [RelayCommand]
    private void Redo()
    {
        ClearSelection();
        if (_redoStack.Count == 0) return;
        var cmd = _redoStack.Pop();
        cmd.Execute();
        _undoStack.Push(cmd);
        UpdateUndoRedoState();
    }

    [RelayCommand]
    private void CopyToClipboard()
    {
        ClearSelection();
        var bitmap = RenderCanvasToBitmap();
        if (bitmap != null)
        {
            ClipboardService.CopyToClipboard(bitmap);
            StatusText = "已复制到剪切板";
        }
    }

    [RelayCommand]
    private void SaveAs()
    {
        ClearSelection();
        var bitmap = RenderCanvasToBitmap();
        if (bitmap != null)
        {
            var path = SaveService.SaveAs(bitmap);
            if (path != null)
                StatusText = $"已保存: {path}";
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        ClearSelection();
        _shapes.Clear();
        _undoStack.Clear();
        _redoStack.Clear();
        _sequenceCounter = 1;
        _shapeVisuals.Clear();
        _canvas?.Children.Clear();
        if (BackgroundImage != null && _canvas != null)
            _canvas.Children.Add(new Image { Source = BackgroundImage });
        UpdateUndoRedoState();
    }

    #endregion

    private void UpdateUndoRedoState()
    {
        CanUndo = _undoStack.Count > 0;
        CanRedo = _redoStack.Count > 0;
    }

    private IAnnotationShape? CreateShape(ShapeType type) => type switch
    {
        ShapeType.Rectangle => new RectangleShape(),
        ShapeType.Ellipse => new EllipseShape(),
        ShapeType.Arrow => new ArrowShape(),
        ShapeType.Line => new LineShape(),
        ShapeType.FreeDraw => new FreeDrawShape(),
        ShapeType.Mosaic => new MosaicShape(),
        ShapeType.SequenceNumber => new SequenceNumberShape(),
        _ => null
    };

    private void CreateTextAnnotation(Point position)
    {
        RequestTextEdit?.Invoke(position, "", (text) =>
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var textShape = new TextAnnotationShape
            {
                StartPoint = position,
                StrokeColor = StrokeColor,
                FontSize = TextFontSize,
                FontFamilyName = TextFontFamily,
                IsBold = TextIsBold,
                IsItalic = TextIsItalic,
                IsUnderline = TextIsUnderline,
                IsStrikethrough = TextIsStrikethrough,
                Text = text
            };
            AddShapeToCanvas(textShape);
            _shapes.Add(textShape);
            _undoStack.Push(new AddShapeCommand(_shapes, textShape, _canvas!));
            _redoStack.Clear();
            UpdateUndoRedoState();
        });
    }

    private BitmapSource? RenderCanvasToBitmap()
    {
        if (_canvas == null || BackgroundImage == null) return null;
        int width = (int)BackgroundImage.PixelWidth;
        int height = (int)BackgroundImage.PixelHeight;

        var renderBitmap = new RenderTargetBitmap(
            width, height, BackgroundImage.DpiX, BackgroundImage.DpiY, PixelFormats.Pbgra32);

        var drawingVisual = new DrawingVisual();
        using (var ctx = drawingVisual.RenderOpen())
        {
            ctx.DrawImage(BackgroundImage, new Rect(0, 0, width, height));
            foreach (var shape in _shapes)
            {
                if (!_shapeVisuals.ContainsKey(shape)) continue;
                foreach (var vis in _shapeVisuals[shape])
                {
                    if (vis is System.Windows.Shapes.Shape s)
                    {
                        double left = Canvas.GetLeft(s), top = Canvas.GetTop(s);
                        if (double.IsNaN(left)) left = 0;
                        if (double.IsNaN(top)) top = 0;
                        ctx.PushTransform(new TranslateTransform(left, top));
                        ctx.DrawGeometry(s.Fill, new Pen(s.Stroke, s.StrokeThickness), s.RenderedGeometry);
                        ctx.Pop();
                    }
                    else if (vis is TextBlock tb)
                    {
                        double left = Canvas.GetLeft(tb), top = Canvas.GetTop(tb);
                        if (double.IsNaN(left)) left = 0;
                        if (double.IsNaN(top)) top = 0;
                        var ft = new FormattedText(
                            tb.Text ?? string.Empty,
                            System.Globalization.CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            new Typeface(tb.FontFamily, tb.FontStyle, tb.FontWeight, tb.FontStretch),
                            tb.FontSize, tb.Foreground,
                            VisualTreeHelper.GetDpi(_canvas).PixelsPerDip);
                        ctx.DrawText(ft, new Point(left, top));
                    }
                    else if (vis is Image img && img.Source != null)
                    {
                        double left = Canvas.GetLeft(img), top = Canvas.GetTop(img);
                        if (double.IsNaN(left)) left = 0;
                        if (double.IsNaN(top)) top = 0;
                        double w = img.ActualWidth > 0 ? img.ActualWidth : img.Width;
                        double h = img.ActualHeight > 0 ? img.ActualHeight : img.Height;
                        ctx.DrawImage(img.Source, new Rect(left, top, w, h));
                    }
                }
            }
        }
        renderBitmap.Render(drawingVisual);
        return renderBitmap;
    }
}
