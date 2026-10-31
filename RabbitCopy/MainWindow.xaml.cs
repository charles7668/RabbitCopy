using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
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
            this, (_, _) => ScrollLogToEnd());
        WeakReferenceMessenger.Default.Register<ElevateRequestMessage>(
            this, (_, _) => Dispatcher.BeginInvoke(() =>
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = Process.GetCurrentProcess().MainModule!.FileName,
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = AppContext.BaseDirectory
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
            this, (_, _) => ScrollLogToEnd());
        WeakReferenceMessenger.Default.Register<ElevateRequestMessage>(
            this, (_, _) => Dispatcher.BeginInvoke(() =>
            {
                ProcessStartInfo startInfo = new()
                {
                    FileName = Process.GetCurrentProcess().MainModule!.FileName,
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WorkingDirectory = AppContext.BaseDirectory
                };
                Process.Start(startInfo);
            }));
    }

    private int _scrollToEndPending;

    /// <summary>
    /// Queues a scroll of the log to its last line.
    /// </summary>
    /// <remarks>
    /// RoboCopy reports its output from background threads, so the bound text reaches
    /// <see cref="TxtLog"/> later, at <see cref="DispatcherPriority.DataBind"/>, and its new
    /// extent later still, during the render pass. Scrolling at the default (Normal) priority
    /// runs before both and only scrolls to the previous end, which looks like no scrolling at
    /// all. Background priority runs after both. The flag coalesces the burst of per-line
    /// requests into a single pending scroll.
    /// </remarks>
    private void ScrollLogToEnd()
    {
        if (Interlocked.Exchange(ref _scrollToEndPending, 1) == 1)
            return;

        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            Interlocked.Exchange(ref _scrollToEndPending, 0);
            // Several lines can share one dispatch, so make sure the extent is up to date.
            TxtLog.UpdateLayout();
            TxtLog.ScrollToEnd();
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

    private void SrcTextBox_OnPreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Link
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void SrcTextBox_OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
        {
            return;
        }

        var appendBackslash = files.Select(src =>
        {
            try
            {
                var attributes = File.GetAttributes(src);
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    src = src.TrimEnd('\\') + "\\";
                }

                return src;
            }
            catch
            {
                // ignore
            }

            return "";
        }).Where(src => !string.IsNullOrWhiteSpace(src));

        if (DataContext is MainWindowViewModel vm)
        {
            vm.SrcText = string.Join("\n", appendBackslash);
        }
    }
}
