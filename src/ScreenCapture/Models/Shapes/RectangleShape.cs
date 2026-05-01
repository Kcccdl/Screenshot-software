using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScreenCapture.Models.Enums;

namespace ScreenCapture.Models.Shapes;

/// <summary>
/// 矩形标注形状
/// 通过拖拽起始点和结束点确定矩形范围
/// </summary>
public class RectangleShape : AnnotationShapeBase
{
    private System.Windows.Shapes.Rectangle? _rect;

    public RectangleShape() => Type = ShapeType.Rectangle;

    /// <summary>构建矩形可视化元素</summary>
    protected override void BuildVisuals()
    {
        _rect = new System.Windows.Shapes.Rectangle
        {
            Stroke = new SolidColorBrush(StrokeColor),
            StrokeThickness = StrokeThickness,
            Fill = FillColor.HasValue ? new SolidColorBrush(FillColor.Value) : null
        };
        ApplyGeometry();
        VisualElements.Add(_rect);
    }

    /// <summary>更新矩形的线条颜色、粗细和位置</summary>
    protected override void UpdateVisuals()
    {
        if (_rect == null) return;
        _rect.Stroke = new SolidColorBrush(StrokeColor);
        _rect.StrokeThickness = StrokeThickness;
        ApplyGeometry();
    }

    /// <summary>将包围矩形的坐标和尺寸应用到 WPF 矩形元素</summary>
    private void ApplyGeometry()
    {
        if (_rect == null) return;
        var r = BoundingRect;
        Canvas.SetLeft(_rect, r.X);
        Canvas.SetTop(_rect, r.Y);
        _rect.Width = r.Width;
        _rect.Height = r.Height;
    }

    /// <summary>命中测试：扩展线条宽度后判断点是否在矩形内</summary>
    public override bool HitTest(Point p)
    {
        var r = BoundingRect;
        var inflated = r;
        inflated.Inflate(StrokeThickness + 3, StrokeThickness + 3);
        return inflated.Contains(p);
    }
}
