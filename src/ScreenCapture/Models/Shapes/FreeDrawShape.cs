using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using ScreenCapture.Models.Enums;

namespace ScreenCapture.Models.Shapes;

/// <summary>
/// 画笔涂鸦标注形状
/// 通过累积鼠标移动轨迹点绘制自由线条
/// </summary>
public class FreeDrawShape : AnnotationShapeBase
{
    /// <summary>轨迹点集合</summary>
    public List<Point> Points { get; } = new();
    private Polyline? _polyline;

    public FreeDrawShape() => Type = ShapeType.FreeDraw;

    /// <summary>添加一个轨迹点并实时更新折线</summary>
    public void AddPoint(Point p)
    {
        Points.Add(p);
        if (_polyline != null)
            _polyline.Points.Add(p);
    }

    /// <summary>构建折线可视化元素</summary>
    protected override void BuildVisuals()
    {
        _polyline = new Polyline
        {
            Stroke = new SolidColorBrush(StrokeColor),
            StrokeThickness = StrokeThickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        foreach (var p in Points)
            _polyline.Points.Add(p);
        VisualElements.Add(_polyline);
    }

    /// <summary>更新折线的颜色和粗细</summary>
    protected override void UpdateVisuals()
    {
        if (_polyline == null) return;
        _polyline.Stroke = new SolidColorBrush(StrokeColor);
        _polyline.StrokeThickness = StrokeThickness;
    }

    /// <summary>命中测试：检查点是否靠近折线的任意一段</summary>
    public override bool HitTest(Point p)
    {
        for (int i = 1; i < Points.Count; i++)
        {
            if (DistanceToLine(p, Points[i - 1], Points[i]) < StrokeThickness + 5)
                return true;
        }
        return false;
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
