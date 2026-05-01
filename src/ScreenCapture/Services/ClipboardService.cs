using System.Windows;
using System.Windows.Media.Imaging;

namespace ScreenCapture.Services;

/// <summary>
/// 剪切板服务
/// 将截图图像复制到系统剪切板
/// </summary>
public static class ClipboardService
{
    /// <summary>
    /// 将图像复制到系统剪切板
    /// 复制后可在任意应用中粘贴（如微信、QQ、画图等）
    /// </summary>
    public static void CopyToClipboard(BitmapSource image)
    {
        Clipboard.SetImage(image);
    }
}
