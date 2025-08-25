using System.CommandLine;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using RabbitCopy.Models;
using RabbitCopy.Services;
using MessageBox = System.Windows.MessageBox;

namespace RabbitCopy;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        PrepareServices();

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
            var openUIOption = new Option<bool>("--open")
            {
                Description = "Open the UI"
            };

            rootCommand.Add(destOption);
            rootCommand.Add(filesOption);
            rootCommand.Add(openUIOption);

            var parseResult = rootCommand.Parse(e.Args);

            var runOptions = new RunOptions
            {
                DestPath = parseResult.GetValue(destOption)?.TrimEnd('\\'),
                SrcPaths = parseResult.GetValue(filesOption),
                OpenUI = parseResult.GetValue(openUIOption)
            };

            if ((runOptions.DestPath == null || runOptions.SrcPaths == null || runOptions.SrcPaths.Length == 0) &&
                !runOptions.OpenUI)
            {
                MessageBox.Show("Please provide a destination and at least one source file.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
                return;
            }

            if (runOptions.DestPath != null)
                runOptions.DestPath += '\\';

            var mainWindow = new MainWindow(runOptions);
            mainWindow.Show();
        }
        else
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }

    private void PrepareServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<AppPathService, AppPathService>();
        services.AddSingleton<ConfigService, ConfigService>();
        services.AddSingleton<IconUpdater, IconUpdater>();
        services.AddSingleton<HistoryService, HistoryService>();
        ServiceProvider = services.BuildServiceProvider();
    }
}