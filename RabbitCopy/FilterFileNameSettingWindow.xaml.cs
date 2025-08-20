using JetBrains.Annotations;
using RabbitCopy.ViewModels;

namespace RabbitCopy;

/// <summary>
/// FilterFileNameSettingWindow.xaml 的互動邏輯
/// </summary>
public partial class FilterFileNameSettingWindow
{
    [UsedImplicitly]
    public FilterFileNameSettingWindow(FilterFileNameSettingWindowViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    [UsedImplicitly]
    public FilterFileNameSettingWindow()
    {
        InitializeComponent();
        DataContext = new FilterFileNameSettingWindowViewModel();
    }
}