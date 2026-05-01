namespace ScreenCapture.Models.Commands;

/// <summary>
/// 标注命令接口（命令模式）
/// 用于支持撤销/重做功能
/// </summary>
public interface IAnnotationCommand
{
    /// <summary>执行命令</summary>
    void Execute();

    /// <summary>撤销命令</summary>
    void Undo();
}
