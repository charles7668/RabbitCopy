using CommunityToolkit.Mvvm.Messaging;
using RabbitCopy.Models;
using RabbitCopy.ViewModels;

namespace RabbitCopy;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow(RunOptions runOptions)
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(runOptions);

        WeakReferenceMessenger.Default.Register<ShutdownRequestMessage>(
            this, (_, _) => Close());
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();

        WeakReferenceMessenger.Default.Register<ShutdownRequestMessage>(
            this, (_, _) => { });
    }
}