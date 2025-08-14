using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;

namespace RabbitCopyInstaller;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private const string CLSID = "{FE6B2057-2C0F-4461-9455-67116990409E}";
    private const string MENU_NAME = "RabbitCopyContextMenu";

    private void Install_Click(object sender, RoutedEventArgs e)
    {
        var workDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var startInfo = new ProcessStartInfo
        {
            FileName = "regsvr32.exe",
            Arguments = $"\"{workDir}\\RabbitCopyContextMenu.comhost.dll\"",
            Verb = "runas",
            WorkingDirectory = workDir,
            UseShellExecute = true,
            CreateNoWindow = false
        };
        var regProc = new Process
        {
            StartInfo = startInfo
        };
        regProc.Start();
        regProc.WaitForExit();

        var rootName = Registry.CurrentUser;

        var subKeyPath = @"Software\Classes\*\shellex\ContextMenuHandlers\" + MENU_NAME;

        using (var subKey = rootName.CreateSubKey(subKeyPath, true))
        {
            subKey.SetValue(string.Empty, CLSID);
        }

        subKeyPath = @"Software\Classes\Directory\Background\shellex\ContextMenuHandlers\" + MENU_NAME;

        using (var subKey = rootName.CreateSubKey(subKeyPath, true))
        {
            subKey.SetValue(string.Empty, CLSID);
        }

        subKeyPath = @"Software\Classes\Folder\shellex\ContextMenuHandlers\" + MENU_NAME;

        using (var subKey = rootName.CreateSubKey(subKeyPath, true))
        {
            subKey.SetValue(string.Empty, CLSID);
        }

        MessageBox.Show("Install Complete");
    }

    private void Uninstall_Click(object sender, RoutedEventArgs e)
    {
        var workDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var startInfo = new ProcessStartInfo
        {
            FileName = "regsvr32.exe",
            Arguments = $"/u \"{workDir}\\RabbitCopyContextMenu.comhost.dll\"",
            Verb = "runas",
            WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            UseShellExecute = true,
            CreateNoWindow = false
        };
        var regProc = new Process
        {
            StartInfo = startInfo
        };
        regProc.Start();
        regProc.WaitForExit();

        var rootName = Registry.CurrentUser;

        var subKeyPath = @"Software\Classes\*\shellex\ContextMenuHandlers\" + MENU_NAME;
        rootName.DeleteSubKey(subKeyPath);
        subKeyPath = @"Software\Classes\Directory\Background\shellex\ContextMenuHandlers\" + MENU_NAME;
        rootName.DeleteSubKey(subKeyPath);
        subKeyPath = @"Software\Classes\Folder\shellex\ContextMenuHandlers\" + MENU_NAME;
        rootName.DeleteSubKey(subKeyPath);

        MessageBox.Show("Uninstall Complete");
    }
}