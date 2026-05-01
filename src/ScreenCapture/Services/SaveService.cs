using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using ScreenCapture.Models.Enums;

namespace ScreenCapture.Services;

/// <summary>
/// 文件保存服务
/// 支持将截图保存为 PNG、JPEG、BMP 格式
/// </summary>
public static class SaveService
{
    /// <summary>
    /// 弹出"另存为"对话框，让用户选择保存路径和格式
    /// </summary>
    /// <returns>保存的文件路径，用户取消时返回 null</returns>
    public static string? SaveAs(BitmapSource image)
    {
        var dialog = new SaveFileDialog
        {
            Filter = "PNG 图片|*.png|JPEG 图片|*.jpg|BMP 图片|*.bmp",
            DefaultExt = ".png",
            FileName = $"截图_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (dialog.ShowDialog() != true) return null;

        // 根据文件扩展名确定保存格式
        var format = Path.GetExtension(dialog.FileName).ToLower() switch
        {
            ".jpg" or ".jpeg" => SaveFormat.Jpg,
            ".bmp" => SaveFormat.Bmp,
            _ => SaveFormat.Png
        };

        SaveToFile(image, dialog.FileName, format);
        return dialog.FileName;
    }

    /// <summary>
    /// 将图像保存到指定文件路径
    /// </summary>
    /// <param name="image">要保存的图像</param>
    /// <param name="filePath">目标文件路径</param>
    /// <param name="format">保存格式</param>
    public static void SaveToFile(BitmapSource image, string filePath, SaveFormat format = SaveFormat.Png)
    {
        // 根据格式创建对应的编码器
        BitmapEncoder encoder = format switch
        {
            SaveFormat.Jpg => new JpegBitmapEncoder { QualityLevel = 95 },
            SaveFormat.Bmp => new BmpBitmapEncoder(),
            _ => new PngBitmapEncoder()
        };
        encoder.Frames.Add(BitmapFrame.Create(image));

        // 确保目标目录存在
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        using var stream = new FileStream(filePath, FileMode.Create);
        encoder.Save(stream);
    }
}
