using System.Windows.Controls;
using ScreenCapture.Models.Shapes;

namespace ScreenCapture.Models.Commands;

/// <summary>
/// 添加标注形状的命令
/// 执行时将形状添加到列表和画布，撤销时移除
/// </summary>
public class AddShapeCommand : IAnnotationCommand
{
    private readonly List<IAnnotationShape> _shapes;  // 标注形状列表
    private readonly IAnnotationShape _shape;          // 要添加的形状
    private readonly Canvas _canvas;                   // 目标画布
    private List<System.Windows.UIElement>? _visuals;  // 缓存的可视化元素

    public AddShapeCommand(List<IAnnotationShape> shapes, IAnnotationShape shape, Canvas canvas)
    {
        _shapes = shapes;
        _shape = shape;
        _canvas = canvas;
    }

    /// <summary>执行：创建可视化元素并添加到画布</summary>
    public void Execute()
    {
        _shapes.Add(_shape);
        _visuals = _shape.CreateVisualElements().ToList();
        foreach (var vis in _visuals)
            _canvas.Children.Add(vis);
    }

    /// <summary>撤销：从画布和列表中移除形状</summary>
    public void Undo()
    {
        _shapes.Remove(_shape);
        if (_visuals != null)
        {
            foreach (var vis in _visuals)
                _canvas.Children.Remove(vis);
        }
    }
}
