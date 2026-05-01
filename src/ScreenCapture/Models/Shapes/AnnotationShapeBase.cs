using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using ScreenCapture.Models.Enums;

namespace ScreenCapture.Models.Shapes;

/// <summary>
/// 标注形状的抽象基类，提供通用属性和方法
/// 所有具体形状类都继承此类
/// </summary>
public abstract class AnnotationShapeBase : IAnnotationShape, INotifyPropertyChanged
{
    /// <summary>形状类型（由子类构造函数设置）</summary>
    public ShapeType Type { get; protected set; }

    /// <summary>线条颜色，默认红色</summary>
    public Color StrokeColor { get; set; } = Colors.Red;

    /// <summary>线条粗细，默认 2 像素</summary>
    public double StrokeThickness { get; set; } = 2.0;

    /// <summary>填充颜色，默认无填充</summary>
    public Color? FillColor { get; set; }

    /// <summary>起始点坐标</summary>
    public Point StartPoint { get; set; }

    /// <summary>结束点坐标</summary>
    public Point EndPoint { get; set; }

    /// <summary>是否被选中</summary>
    public bool IsSelected { get; set; }

    /// <summary>缓存的可视化元素列表</summary>
    protected readonly List<UIElement> VisualElements = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 创建可视化元素（清空旧元素后重新构建）
    /// </summary>
    public IReadOnlyList<UIElement> CreateVisualElements()
    {
        VisualElements.Clear();
        BuildVisuals();
        return VisualElements.AsReadOnly();
    }

    /// <summary>
    /// 更新可视化元素的属性（位置、颜色等）
    /// </summary>
    public void UpdateVisualElements()
    {
        UpdateVisuals();
    }

    /// <summary>子类实现：构建可视化元素</summary>
    protected abstract void BuildVisuals();

    /// <summary>子类实现：更新可视化元素</summary>
    protected abstract void UpdateVisuals();

    /// <summary>子类实现：命中测试</summary>
    public abstract bool HitTest(Point point);

    /// <summary>
    /// 根据起始点和结束点计算包围矩形（自动处理反向拖拽）
    /// </summary>
    protected Rect BoundingRect => new(
        Math.Min(StartPoint.X, EndPoint.X),
        Math.Min(StartPoint.Y, EndPoint.Y),
        Math.Abs(EndPoint.X - StartPoint.X),
        Math.Abs(EndPoint.Y - StartPoint.Y));

    /// <summary>触发属性变更通知</summary>
    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
