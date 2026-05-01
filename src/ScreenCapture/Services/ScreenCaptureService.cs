using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ScreenCapture.Services;

/// <summary>
/// 屏幕截图服务
/// 使用 GDI+ 的 CopyFromScreen 方法捕获屏幕图像
/// </summary>
public class ScreenCaptureService
{
    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    /// <summary>
    /// 捕获整个主屏幕
    /// 返回冻结的 BitmapSource（可跨线程使用）
    /// </summary>
    public BitmapSource CaptureFullScreen()
    {
        // 使用 Screen 类获取屏幕尺寸（比 SystemParameters 更可靠）
        int w = (int)SystemParameters.PrimaryScreenWidth;
        int h = (int)SystemParameters.PrimaryScreenHeight;

        // 使用 GDI+ 截图
        using var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(w, h), CopyPixelOperation.SourceCopy);
        }

        // 转换为 WPF BitmapSource（通过内存流，确保可冻结）
        return BitmapToImageSource(bmp);
    }

    /// <summary>
    /// 从全屏位图裁剪指定区域
    /// 使用内存流方式，避免 CroppedBitmap 的兼容性问题
    /// </summary>
    public BitmapSource CropRegion(BitmapSource fullBitmap, int x, int y, int width, int height)
    {
        // 安全限制坐标范围
        x = Math.Max(0, x);
        y = Math.Max(0, y);
        if (x + width > fullBitmap.PixelWidth) width = fullBitmap.PixelWidth - x;
        if (y + height > fullBitmap.PixelHeight) height = fullBitmap.PixelHeight - y;
        if (width < 1 || height < 1)
            throw new ArgumentException("裁剪区域无效");

        // 使用 CroppedBitmap 并冻结
        var cropped = new CroppedBitmap(fullBitmap, new Int32Rect(x, y, width, height));
        cropped.Freeze();
        return cropped;
    }

    /// <summary>
    /// 将 GDI+ Bitmap 转换为 WPF BitmapSource
    /// 通过 PNG 内存流确保格式兼容
    /// </summary>
    private static BitmapSource BitmapToImageSource(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        stream.Position = 0;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze(); // 冻结以支持跨线程访问
        return image;
    }
}
