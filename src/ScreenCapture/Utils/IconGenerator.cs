using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ScreenCapture.Utils;

/// <summary>
/// 应用图标生成器
/// 在运行时生成一个好看的截图工具图标
/// </summary>
public static class IconGenerator
{
    /// <summary>
    /// 生成应用图标并保存为 .ico 文件
    /// 返回图标文件路径
    /// </summary>
    public static string GenerateIcon()
    {
        string iconPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "app_icon.ico");

        // 如果已存在则直接返回
        if (File.Exists(iconPath)) return iconPath;

        using var bmp = new Bitmap(256, 256);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        // 绘制圆角矩形背景（渐变蓝色）
        using var bgPath = RoundedRect(4, 4, 248, 248, 40);
        using var bgBrush = new LinearGradientBrush(
            new Point(0, 0), new Point(256, 256),
            Color.FromArgb(0, 120, 212), Color.FromArgb(0, 90, 160));
        g.FillPath(bgBrush, bgPath);

        // 绘制剪刀图标（简化版）
        using var pen = new Pen(Color.White, 10);
        pen.StartCap = LineCap.Round;
        pen.EndCap = LineCap.Round;

        // 剪刀的两条交叉线
        g.DrawArc(pen, 70, 50, 60, 60, 0, 360);
        g.DrawArc(pen, 126, 50, 60, 60, 0, 360);

        // 剪刀的下半部分
        using var pen2 = new Pen(Color.White, 8);
        pen2.StartCap = LineCap.Round;
        pen2.EndCap = LineCap.Round;
        g.DrawLine(pen2, 100, 110, 80, 200);
        g.DrawLine(pen2, 156, 110, 176, 200);

        // 保存为 .ico
        SaveAsIcon(bmp, iconPath);
        return iconPath;
    }

    /// <summary>创建圆角矩形路径</summary>
    private static GraphicsPath RoundedRect(int x, int y, int w, int h, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(x, y, d, d, 180, 90);
        path.AddArc(x + w - d, y, d, d, 270, 90);
        path.AddArc(x + w - d, y + h - d, d, d, 0, 90);
        path.AddArc(x, y + h - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>将 Bitmap 保存为 .ico 文件</summary>
    private static void SaveAsIcon(Bitmap bmp, string path)
    {
        using var ms = new MemoryStream();
        // 写入 .ico 文件头
        ms.Write([0, 0]);          // reserved
        ms.Write([1, 0]);          // type = icon
        ms.Write([1, 0]);          // count = 1
        ms.Write([0]);             // width (0 = 256)
        ms.Write([0]);             // height (0 = 256)
        ms.Write([0]);             // color palette
        ms.Write([0]);             // reserved
        ms.Write([1, 0]);          // color planes
        ms.Write([32, 0]);         // bits per pixel

        // 将位图转为 PNG 数据嵌入
        using var pngMs = new MemoryStream();
        bmp.Save(pngMs, ImageFormat.Png);
        byte[] pngData = pngMs.ToArray();

        // 数据大小
        byte[] sizeBytes = BitConverter.GetBytes(pngData.Length);
        ms.Write(sizeBytes);

        // 数据偏移（6 header + 16 entry = 22）
        ms.Write([22, 0, 0, 0]);

        // PNG 数据
        ms.Write(pngData);

        File.WriteAllBytes(path, ms.ToArray());
    }
}
