using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScreenCapture.Models.Enums;

namespace ScreenCapture.Models.Shapes;

/// <summary>
/// 椭圆/圆形标注形状
/// 通过拖拽起始点和结束点确定椭圆的外接矩形
/// </summary>
public class EllipseShape : AnnotationShapeBase
{
    private System.Windows.Shapes.Ellipse? _ellipse;

    public EllipseShape() => Type = ShapeType.Ellipse;

    /// <summary>构建椭圆可视化元素</summary>
    protected override void BuildVisuals()
    {
        _ellipse = new System.Windows.Shapes.Ellipse
        {
            Stroke = new SolidColorBrush(StrokeColor),
            StrokeThickness = StrokeThickness,
            Fill = FillColor.HasValue ? new SolidColorBrush(FillColor.Value) : null
        };
        ApplyGeometry();
        VisualElements.Add(_ellipse);
    }

    /// <summary>更新椭圆的线条颜色、粗细和位置</summary>
    protected override void UpdateVisuals()
    {
        if (_ellipse == null) return;
        _ellipse.Stroke = new SolidColorBrush(StrokeColor);
        _ellipse.StrokeThickness = StrokeThickness;
        ApplyGeometry();
    }

    /// <summary>将包围矩形的坐标和尺寸应用到 WPF 椭圆元素</summary>
    private void ApplyGeometry()
    {
        if (_ellipse == null) return;
        var r = BoundingRect;
        Canvas.SetLeft(_ellipse, r.X);
        Canvas.SetTop(_ellipse, r.Y);
        _ellipse.Width = r.Width;
        _ellipse.Height = r.Height;
    }

    /// <summary>命中测试：扩展线条宽度后判断点是否在椭圆内</summary>
    public override bool HitTest(Point p)
    {
        var r = BoundingRect;
        var inflated = r;
        inflated.Inflate(StrokeThickness + 3, StrokeThickness + 3);
        return inflated.Contains(p);
    }
}
