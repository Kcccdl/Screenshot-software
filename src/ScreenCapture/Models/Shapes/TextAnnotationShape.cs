using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ScreenCapture.Models.Enums;

namespace ScreenCapture.Models.Shapes;

/// <summary>
/// 文字标注形状
/// 支持字体、字号、粗体、斜体、下划线、删除线
/// </summary>
public class TextAnnotationShape : AnnotationShapeBase
{
    public string Text { get; set; } = string.Empty;
    public double FontSize { get; set; } = 16.0;
    public string FontFamilyName { get; set; } = "Microsoft YaHei";
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public bool IsUnderline { get; set; }
    public bool IsStrikethrough { get; set; }

    private TextBlock? _textBlock;

    public TextAnnotationShape() => Type = ShapeType.Text;

    protected override void BuildVisuals()
    {
        _textBlock = new TextBlock
        {
            Text = Text,
            FontSize = FontSize,
            FontFamily = new FontFamily(FontFamilyName),
            FontWeight = IsBold ? FontWeights.Bold : FontWeights.Normal,
            FontStyle = IsItalic ? FontStyles.Italic : FontStyles.Normal,
            Foreground = new SolidColorBrush(StrokeColor),
            TextDecorations = BuildTextDecorations(),
        };
        Canvas.SetLeft(_textBlock, StartPoint.X);
        Canvas.SetTop(_textBlock, StartPoint.Y);
        VisualElements.Add(_textBlock);
        _textBlock.Loaded += (s, e) => UpdateEndPoint();
    }

    protected override void UpdateVisuals()
    {
        if (_textBlock == null) return;
        _textBlock.Text = Text;
        _textBlock.FontSize = FontSize;
        _textBlock.FontFamily = new FontFamily(FontFamilyName);
        _textBlock.FontWeight = IsBold ? FontWeights.Bold : FontWeights.Normal;
        _textBlock.FontStyle = IsItalic ? FontStyles.Italic : FontStyles.Normal;
        _textBlock.Foreground = new SolidColorBrush(StrokeColor);
        _textBlock.TextDecorations = BuildTextDecorations();
        Canvas.SetLeft(_textBlock, StartPoint.X);
        Canvas.SetTop(_textBlock, StartPoint.Y);
        UpdateEndPoint();
    }

    /// <summary>根据设置构建文字装饰（下划线、删除线）</summary>
    private TextDecorationCollection? BuildTextDecorations()
    {
        if (!IsUnderline && !IsStrikethrough) return null;
        var decorations = new TextDecorationCollection();
        if (IsUnderline)
            decorations.Add(TextDecorations.Underline[0]);
        if (IsStrikethrough)
            decorations.Add(TextDecorations.Strikethrough[0]);
        return decorations;
    }

    private void UpdateEndPoint()
    {
        if (_textBlock == null) return;
        double w = _textBlock.ActualWidth;
        double h = _textBlock.ActualHeight;
        if (w < 1) w = Math.Max(40, Text.Length * FontSize * 0.65);
        if (h < 1) h = FontSize * 1.4;
        EndPoint = new Point(StartPoint.X + w, StartPoint.Y + h);
    }

    public override bool HitTest(Point p)
    {
        if (_textBlock == null) return false;
        double left = Canvas.GetLeft(_textBlock);
        double top = Canvas.GetTop(_textBlock);
        double w = _textBlock.ActualWidth;
        double h = _textBlock.ActualHeight;
        if (w < 1) w = Math.Max(40, Text.Length * FontSize * 0.65);
        if (h < 1) h = FontSize * 1.4;
        var rect = new Rect(left - 4, top - 4, w + 8, h + 8);
        return rect.Contains(p);
    }
}
