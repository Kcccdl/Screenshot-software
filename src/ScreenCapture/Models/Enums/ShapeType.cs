namespace ScreenCapture.Models.Enums;

/// <summary>
/// 标注形状类型枚举
/// </summary>
public enum ShapeType
{
    /// <summary>矩形框</summary>
    Rectangle,
    /// <summary>椭圆/圆形</summary>
    Ellipse,
    /// <summary>箭头</summary>
    Arrow,
    /// <summary>直线</summary>
    Line,
    /// <summary>画笔涂鸦</summary>
    FreeDraw,
    /// <summary>文字标注</summary>
    Text,
    /// <summary>马赛克/模糊</summary>
    Mosaic,
    /// <summary>序号标注（带数字的圆圈）</summary>
    SequenceNumber
}
