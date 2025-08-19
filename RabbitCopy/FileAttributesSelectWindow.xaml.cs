using RabbitCopy.ViewModels;

namespace RabbitCopy;

/// <summary>
/// FileAttributesSelectWindow.xaml 的互動邏輯
/// </summary>
public partial class FileAttributesSelectWindow
{
    public FileAttributesSelectWindow(FileAttributesSelectWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public FileAttributesSelectWindow()
    {
        InitializeComponent();
        DataContext = new FileAttributesSelectWindowViewModel();
    }
}