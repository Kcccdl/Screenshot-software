using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ScreenCapture.Services;
using ScreenCapture.Themes;
using ScreenCapture.Utils;

namespace ScreenCapture;

/// <summary>
/// 应用程序入口类
/// 管理截图服务和截图流程
/// </summary>
public partial class App : Application
{
    /// <summary>屏幕截图服务实例</summary>
    private readonly ScreenCaptureService _captureService = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ThemeManager.Initialize();

        // 生成并设置应用图标
        try
        {
            var iconPath = IconGenerator.GenerateIcon();
            if (File.Exists(iconPath))
            {
                var icon = new System.Drawing.Icon(iconPath);
                // 通过 MainWindow 设置图标
            }
        }
        catch { }
    }

    /// <summary>
    /// 开始截图流程
    /// </summary>
    /// <param name="restoreCallback">截图完成或取消后恢复主窗口的回调</param>
    public void StartScreenshot(Action? restoreCallback = null)
    {
        try
        {
            // 捕获全屏
            var fullBitmap = _captureService.CaptureFullScreen();

            // 打开选区覆盖层
            var overlay = new Windows.SelectionOverlayWindow(fullBitmap);
            bool selectionMade = false;

            overlay.SelectionConfirmed += (rect) =>
            {
                selectionMade = true;
                try
                {
                    // 裁剪选区（使用服务的安全裁剪方法）
                    var cropped = _captureService.CropRegion(
                        fullBitmap,
                        (int)rect.X, (int)rect.Y,
                        (int)rect.Width, (int)rect.Height);

                    // 打开标注编辑器
                    var editor = new Windows.AnnotationEditorWindow(cropped);
                    editor.Closed += (s, e) => restoreCallback?.Invoke();
                    editor.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开编辑器失败:\n{ex.Message}\n\n{ex.StackTrace}", "错误");
                    restoreCallback?.Invoke();
                }
            };

            overlay.Closed += (s, e) =>
            {
                if (!selectionMade)
                    restoreCallback?.Invoke();
            };

            overlay.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"截图失败:\n{ex.Message}\n\n{ex.StackTrace}", "错误");
            restoreCallback?.Invoke();
        }
    }
}
