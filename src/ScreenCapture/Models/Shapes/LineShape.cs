using System.Windows;
using System.Windows.Media;
using ScreenCapture.Models.Enums;

namespace ScreenCapture.Models.Shapes;

/// <summary>
/// 直线标注形状
/// 从起始点画到结束点，支持圆头线帽
/// </summary>
public class LineShape : AnnotationShapeBase
{
    private System.Windows.Shapes.Line? _line;

    public LineShape() => Type = ShapeType.Line;

    /// <summary>构建直线可视化元素</summary>
    protected override void BuildVisuals()
    {
        _line = new System.Windows.Shapes.Line
        {
            X1 = StartPoint.X, Y1 = StartPoint.Y,
            X2 = EndPoint.X, Y2 = EndPoint.Y,
            Stroke = new SolidColorBrush(StrokeColor),
            StrokeThickness = StrokeThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        VisualElements.Add(_line);
    }

    /// <summary>更新直线的端点坐标和样式</summary>
    protected override void UpdateVisuals()
    {
        if (_line == null) return;
        _line.X1 = StartPoint.X; _line.Y1 = StartPoint.Y;
        _line.X2 = EndPoint.X;   _line.Y2 = EndPoint.Y;
        _line.Stroke = new SolidColorBrush(StrokeColor);
        _line.StrokeThickness = StrokeThickness;
    }

    /// <summary>命中测试：计算点到直线段的距离</summary>
    public override bool HitTest(Point p)
    {
        return DistanceToLine(p, StartPoint, EndPoint) < StrokeThickness + 5;
    }

    /// <summary>计算点 p 到线段 ab 的最短距离</summary>
    private static double DistanceToLine(Point p, Point a, Point b)
    {
        double dx = b.X - a.X, dy = b.Y - a.Y;
        double lenSq = dx * dx + dy * dy;
        if (lenSq == 0) return (p - a).Length;
        // 投影参数 t，限制在 [0,1] 范围内（线段上）
        double t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq));
        var proj = new Point(a.X + t * dx, a.Y + t * dy);
        return (p - proj).Length;
    }
}
