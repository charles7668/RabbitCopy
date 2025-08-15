using RabbitCopy.ViewModels;

namespace RabbitCopy;

/// <summary>
/// FilePropertySelectWindow.xaml 的互動邏輯
/// </summary>
public partial class FilePropertySelectWindow
{
    public FilePropertySelectWindow(FilePropertySelectWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public FilePropertySelectWindow()
    {
        InitializeComponent();
        DataContext = new FilePropertySelectWindowViewModel();
    }
}