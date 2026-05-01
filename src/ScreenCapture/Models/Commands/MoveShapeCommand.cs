using System.Windows;
using ScreenCapture.Models.Shapes;

namespace ScreenCapture.Models.Commands;

/// <summary>
/// 移动标注形状的命令
/// 记录移动前后的坐标，支持撤销到原位
/// </summary>
public class MoveShapeCommand : IAnnotationCommand
{
    private readonly IAnnotationShape _shape;
    private readonly Point _oldStart, _oldEnd;  // 移动前的坐标
    private readonly Point _newStart, _newEnd;  // 移动后的坐标

    public MoveShapeCommand(IAnnotationShape shape, Point oldStart, Point oldEnd, Point newStart, Point newEnd)
    {
        _shape = shape;
        _oldStart = oldStart; _oldEnd = oldEnd;
        _newStart = newStart; _newEnd = newEnd;
    }

    /// <summary>执行：移动到新位置</summary>
    public void Execute()
    {
        _shape.StartPoint = _newStart;
        _shape.EndPoint = _newEnd;
        _shape.UpdateVisualElements();
    }

    /// <summary>撤销：恢复到旧位置</summary>
    public void Undo()
    {
        _shape.StartPoint = _oldStart;
        _shape.EndPoint = _oldEnd;
        _shape.UpdateVisualElements();
    }
}
