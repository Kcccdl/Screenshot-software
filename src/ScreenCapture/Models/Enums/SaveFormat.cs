namespace ScreenCapture.Models.Enums;

/// <summary>
/// 图片保存格式枚举
/// </summary>
public enum SaveFormat
{
    /// <summary>PNG 格式（无损压缩，支持透明）</summary>
    Png,
    /// <summary>JPEG 格式（有损压缩，文件较小）</summary>
    Jpg,
    /// <summary>BMP 格式（无压缩，文件较大）</summary>
    Bmp
}
