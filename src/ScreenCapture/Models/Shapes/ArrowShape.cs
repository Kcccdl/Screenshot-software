using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using ScreenCapture.Models.Enums;

namespace ScreenCapture.Models.Shapes;

/// <summary>
/// 箭头标注形状
/// 由直线 + 三角形箭头组成，箭头方向指向结束点
/// </summary>
public class ArrowShape : AnnotationShapeBase
{
    /// <summary>箭头三角形的长度</summary>
    public double ArrowHeadLength { get; set; } = 15.0;

    /// <summary>箭头三角形的宽度（一半）</summary>
    public double ArrowHeadWidth { get; set; } = 8.0;

    private System.Windows.Shapes.Line? _line;
    private Polygon? _arrowHead;

    public ArrowShape() => Type = ShapeType.Arrow;

    /// <summary>构建箭头可视化元素（直线 + 三角形箭头）</summary>
    protected override void BuildVisuals()
    {
        // 创建箭头主线
        _line = new System.Windows.Shapes.Line
        {
            X1 = StartPoint.X, Y1 = StartPoint.Y,
            X2 = EndPoint.X, Y2 = EndPoint.Y,
            Stroke = new SolidColorBrush(StrokeColor),
            StrokeThickness = StrokeThickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };

        // 创建箭头三角形
        _arrowHead = new Polygon
        {
            Fill = new SolidColorBrush(StrokeColor)
        };
        UpdateArrowHead();

        VisualElements.Add(_line);
        VisualElements.Add(_arrowHead);
    }

    /// <summary>更新箭头的端点坐标和箭头方向</summary>
    protected override void UpdateVisuals()
    {
        if (_line == null || _arrowHead == null) return;
        _line.X1 = StartPoint.X; _line.Y1 = StartPoint.Y;
        _line.X2 = EndPoint.X;   _line.Y2 = EndPoint.Y;
        _line.Stroke = new SolidColorBrush(StrokeColor);
        _line.StrokeThickness = StrokeThickness;
        _arrowHead.Fill = new SolidColorBrush(StrokeColor);
        UpdateArrowHead();
    }

    /// <summary>
    /// 计算并更新箭头三角形的三个顶点
    /// 使用向量数学：沿直线方向 + 垂直方向偏移
    /// </summary>
    private void UpdateArrowHead()
    {
        if (_arrowHead == null) return;
        var dir = new Vector(EndPoint.X - StartPoint.X, EndPoint.Y - StartPoint.Y);
        if (dir.Length < 1) return;
        dir.Normalize();
        var perp = new Vector(-dir.Y, dir.X); // 垂直方向

        var tip = EndPoint;
        var left = new Point(
            EndPoint.X - dir.X * ArrowHeadLength + perp.X * ArrowHeadWidth,
            EndPoint.Y - dir.Y * ArrowHeadLength + perp.Y * ArrowHeadWidth);
        var right = new Point(
            EndPoint.X - dir.X * ArrowHeadLength - perp.X * ArrowHeadWidth,
            EndPoint.Y - dir.Y * ArrowHeadLength - perp.Y * ArrowHeadWidth);

        _arrowHead.Points.Clear();
        _arrowHead.Points.Add(tip);
        _arrowHead.Points.Add(left);
        _arrowHead.Points.Add(right);
    }

    /// <summary>命中测试：计算点到箭头线段的距离</summary>
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
        double t = Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq));
        var proj = new Point(a.X + t * dx, a.Y + t * dy);
        return (p - proj).Length;
    }
}
