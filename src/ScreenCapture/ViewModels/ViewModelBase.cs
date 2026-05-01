using CommunityToolkit.Mvvm.ComponentModel;

namespace ScreenCapture.ViewModels;

/// <summary>
/// 所有 ViewModel 的基类
/// 继承 CommunityToolkit.Mvvm 的 ObservableObject，提供属性变更通知
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}
