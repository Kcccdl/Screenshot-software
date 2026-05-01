using System.Windows;
using System.Windows.Media;
using ScreenCapture.Models.Enums;

namespace ScreenCapture.Models.Shapes;

/// <summary>
/// 标注形状的核心接口
/// 所有标注类型（矩形、箭头、文字等）都必须实现此接口
/// </summary>
public interface IAnnotationShape
{
    /// <summary>形状类型</summary>
    ShapeType Type { get; }

    /// <summary>线条颜色</summary>
    Color StrokeColor { get; set; }

    /// <summary>线条粗细</summary>
    double StrokeThickness { get; set; }

    /// <summary>填充颜色（可选）</summary>
    Color? FillColor { get; set; }

    /// <summary>起始点（鼠标按下位置）</summary>
    Point StartPoint { get; set; }

    /// <summary>结束点（鼠标释放位置）</summary>
    Point EndPoint { get; set; }

    /// <summary>是否被选中</summary>
    bool IsSelected { get; set; }

    /// <summary>
    /// 创建形状的可视化元素，返回可添加到 Canvas 的 UIElement 列表
    /// </summary>
    IReadOnlyList<UIElement> CreateVisualElements();

    /// <summary>
    /// 更新可视化元素（当属性变化时调用）
    /// </summary>
    void UpdateVisualElements();

    /// <summary>
    /// 命中测试：判断给定点是否在形状范围内
    /// </summary>
    bool HitTest(Point point);
}
