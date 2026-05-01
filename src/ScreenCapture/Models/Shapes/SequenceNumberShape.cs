using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using ScreenCapture.Models.Enums;

namespace ScreenCapture.Models.Shapes;

/// <summary>
/// 序号标注形状
/// 显示一个带数字的彩色圆圈，数字自动递增
/// 常用于步骤说明或问题标记
/// </summary>
public class SequenceNumberShape : AnnotationShapeBase
{
    /// <summary>序号数字</summary>
    public int SequenceNumber { get; set; }

    /// <summary>圆圈半径</summary>
    public double Radius { get; set; } = 14.0;

    private Ellipse? _circle;
    private TextBlock? _text;

    public SequenceNumberShape() => Type = ShapeType.SequenceNumber;

    /// <summary>构建圆圈 + 数字的可视化元素</summary>
    protected override void BuildVisuals()
    {
        // 创建彩色圆圈背景
        _circle = new Ellipse
        {
            Width = Radius * 2, Height = Radius * 2,
            Fill = new SolidColorBrush(StrokeColor),
            Stroke = new SolidColorBrush(Colors.White),
            StrokeThickness = 2
        };
        Canvas.SetLeft(_circle, StartPoint.X - Radius);
        Canvas.SetTop(_circle, StartPoint.Y - Radius);

        // 创建序号数字
        _text = new TextBlock
        {
            Text = SequenceNumber.ToString(),
            FontSize = Radius * 1.1,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Colors.White),
            TextAlignment = TextAlignment.Center,
            Width = Radius * 2
        };
        Canvas.SetLeft(_text, StartPoint.X - Radius);
        Canvas.SetTop(_text, StartPoint.Y - Radius * 0.75);

        VisualElements.Add(_circle);
        VisualElements.Add(_text);
    }

    /// <summary>更新圆圈颜色和序号文字</summary>
    protected override void UpdateVisuals()
    {
        if (_circle == null || _text == null) return;
        _circle.Fill = new SolidColorBrush(StrokeColor);
        _text.Text = SequenceNumber.ToString();
        Canvas.SetLeft(_circle, StartPoint.X - Radius);
        Canvas.SetTop(_circle, StartPoint.Y - Radius);
        Canvas.SetLeft(_text, StartPoint.X - Radius);
        Canvas.SetTop(_text, StartPoint.Y - Radius * 0.75);
    }

    /// <summary>命中测试：判断点是否在圆圈内</summary>
    public override bool HitTest(Point p)
    {
        double dx = p.X - StartPoint.X, dy = p.Y - StartPoint.Y;
        return dx * dx + dy * dy <= Radius * Radius;
    }
}
