using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;
using CommunityToolkit.Mvvm.Messaging;
using RabbitCopy.Models;
using RabbitCopy.ViewModels;
using Button = System.Windows.Controls.Button;

namespace RabbitCopy;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow(RunOptions runOptions)
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(this, runOptions);

        WeakReferenceMessenger.Default.Register<ShutdownRequestMessage>(
            this, (_, _) => Close());
        WeakReferenceMessenger.Default.Register<ScrollToEndRequestMessage>(
            this, (_, _) => Dispatcher.BeginInvoke(() => { TxtLog.ScrollToEnd(); }));
        WeakReferenceMessenger.Default.Register<ElevateRequestMessage>(
            this, (_, _) => Dispatcher.BeginInvoke(() =>
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = Process.GetCurrentProcess().MainModule!.FileName,
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                Process.Start(startInfo);
            }));
    }

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel(this);

        WeakReferenceMessenger.Default.Register<ShutdownRequestMessage>(
            this, (_, _) => { });
        WeakReferenceMessenger.Default.Register<ScrollToEndRequestMessage>(
            this, (_, _) => Dispatcher.BeginInvoke(() => { TxtLog.ScrollToEnd(); }));
        WeakReferenceMessenger.Default.Register<ElevateRequestMessage>(
            this, (_, _) => Dispatcher.BeginInvoke(() =>
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = Process.GetCurrentProcess().MainModule!.FileName,
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };
                Process.Start(startInfo);
            }));
    }

    private void DstHistoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        DstHistoryContextMenu.PlacementTarget = (Button)sender;
        DstHistoryContextMenu.Placement = PlacementMode.Bottom;
        DstHistoryContextMenu.IsOpen = true;
    }

    private void SrcHistoryButton_OnClick(object sender, RoutedEventArgs e)
    {
        SrcHistoryContextMenu.PlacementTarget = (Button)sender;
        SrcHistoryContextMenu.Placement = PlacementMode.Bottom;
        SrcHistoryContextMenu.IsOpen = true;
    }
}