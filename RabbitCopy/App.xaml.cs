using System.CommandLine;
using System.Windows;
using RabbitCopy.Models;

namespace RabbitCopy;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);


        if (e.Args.Length > 0)
        {
            var rootCommand = new RootCommand();

            var destOption = new Option<string>(
                "--dest", "-d")
            {
                Description = "Destination directory to copy files to"
            };
            var filesOption = new Option<string[]>(
                "--files", "-f")
            {
                Description = "List of files to copy"
            };

            rootCommand.Add(destOption);
            rootCommand.Add(filesOption);

            var parseResult = rootCommand.Parse(e.Args);

            var runOptions = new RunOptions
            {
                DestPath = parseResult.GetValue(destOption),
                SrcPaths = parseResult.GetValue(filesOption)
            };

            if (runOptions.DestPath == null || runOptions.SrcPaths == null || runOptions.SrcPaths.Length == 0)
            {
                MessageBox.Show("Please provide a destination and at least one source file.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow(runOptions);
            mainWindow.Show();
        }
        else
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}