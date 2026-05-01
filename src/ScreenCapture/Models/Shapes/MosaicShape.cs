using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenCapture.Models.Enums;

namespace ScreenCapture.Models.Shapes;

/// <summary>
/// 马赛克/模糊标注形状
/// 对指定区域的像素进行块平均化处理，实现马赛克效果
/// </summary>
public class MosaicShape : AnnotationShapeBase
{
    /// <summary>马赛克块大小（像素）</summary>
    public int BlockSize { get; set; } = 10;

    /// <summary>原始截图位图（用于提取像素数据）</summary>
    public BitmapSource? SourceBitmap { get; set; }

    private Image? _image;

    public MosaicShape() => Type = ShapeType.Mosaic;

    /// <summary>构建马赛克图像可视化元素</summary>
    protected override void BuildVisuals()
    {
        _image = new Image();
        ApplyMosaic();
        VisualElements.Add(_image);
    }

    /// <summary>重新应用马赛克效果</summary>
    protected override void UpdateVisuals()
    {
        ApplyMosaic();
    }

    /// <summary>
    /// 对选定区域应用马赛克效果
    /// 1. 从原始位图裁剪出选定区域
    /// 2. 对像素进行块平均化
    /// 3. 将处理后的图像显示在画布上
    /// </summary>
    private void ApplyMosaic()
    {
        if (_image == null || SourceBitmap == null) return;
        var rect = BoundingRect;
        if (rect.Width < 1 || rect.Height < 1) return;

        // 限制在源位图范围内
        int sx = Math.Max(0, (int)rect.X);
        int sy = Math.Max(0, (int)rect.Y);
        int sw = Math.Min((int)rect.Width, SourceBitmap.PixelWidth - sx);
        int sh = Math.Min((int)rect.Height, SourceBitmap.PixelHeight - sy);
        if (sw < 1 || sh < 1) return;

        var cropped = new CroppedBitmap(SourceBitmap, new Int32Rect(sx, sy, sw, sh));
        var pixelated = Pixelate(cropped, BlockSize);

        _image.Source = pixelated;
        Canvas.SetLeft(_image, rect.X);
        Canvas.SetTop(_image, rect.Y);
        _image.Width = rect.Width;
        _image.Height = rect.Height;
    }

    /// <summary>
    /// 像素化算法：将图像分成 blockSize x blockSize 的块，
    /// 每个块内所有像素取平均值
    /// </summary>
    private static BitmapSource Pixelate(BitmapSource source, int blockSize)
    {
        int w = source.PixelWidth, h = source.PixelHeight;
        var pixels = new byte[w * h * 4];
        source.CopyPixels(pixels, w * 4, 0);

        // 遍历每个块
        for (int by = 0; by < h; by += blockSize)
        {
            for (int bx = 0; bx < w; bx += blockSize)
            {
                int r = 0, g = 0, b = 0, a = 0, count = 0;
                // 累加块内所有像素
                for (int y = by; y < Math.Min(by + blockSize, h); y++)
                {
                    for (int x = bx; x < Math.Min(bx + blockSize, w); x++)
                    {
                        int idx = (y * w + x) * 4;
                        b += pixels[idx]; g += pixels[idx + 1];
                        r += pixels[idx + 2]; a += pixels[idx + 3];
                        count++;
                    }
                }
                if (count == 0) continue;
                // 计算平均值并填充整个块
                b /= count; g /= count; r /= count; a /= count;
                for (int y = by; y < Math.Min(by + blockSize, h); y++)
                {
                    for (int x = bx; x < Math.Min(bx + blockSize, w); x++)
                    {
                        int idx = (y * w + x) * 4;
                        pixels[idx] = (byte)b; pixels[idx + 1] = (byte)g;
                        pixels[idx + 2] = (byte)r; pixels[idx + 3] = (byte)a;
                    }
                }
            }
        }

        var result = BitmapSource.Create(w, h, source.DpiX, source.DpiY,
            source.Format, source.Palette, pixels, w * 4);
        result.Freeze(); // 冻结以支持跨线程访问
        return result;
    }

    /// <summary>命中测试：判断点是否在马赛克区域内</summary>
    public override bool HitTest(Point p)
    {
        return BoundingRect.Contains(p);
    }
}
