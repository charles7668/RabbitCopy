using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RabbitCopy.Helper;
using RabbitCopy.Models;
using RabbitCopy.RoboCopyModule;
using MessageBox = HandyControl.Controls.MessageBox;

namespace RabbitCopy.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _copyLog = string.Empty;

    [ObservableProperty]
    private string _destText = string.Empty;

    [ObservableProperty]
    private bool _excludeEmptyDirsOption;

    [ObservableProperty]
    private string _srcText = string.Empty;

    [ObservableProperty]
    private BitmapImage? _windowIcon;

    private RunOptions? _runOptions;

    public MainWindowViewModel(RunOptions runOptions)
    {
        _runOptions = runOptions;
        _srcText = string.Join("\n", runOptions.SrcPaths ?? []);
        _destText = runOptions.DestPath ?? string.Empty;
    }

    public MainWindowViewModel()
    {
        WindowIcon = ImageHelper.ByteArrayToBitmapImage(IconResource.rabbit_32x32);
    }

    [RelayCommand]
    private async Task WindowLoaded()
    {
        // normal launch
        if (_runOptions is null)
            return;

        await DryRunCopy();

        ShutDown();
    }

    private void ShutDown()
    {
        WeakReferenceMessenger.Default.Send(new ShutdownRequestMessage());
    }

    private async Task Copy(bool dryRun)
    {
        if (string.IsNullOrWhiteSpace(SrcText) || string.IsNullOrWhiteSpace(DestText))
        {
            MessageBox.Show("Please select sources and destination", "Error", icon: MessageBoxImage.Error);
            return;
        }

        var srcList = SrcText.Replace("\r", "").Split('\n').ToList();
        var srcGroup = new Dictionary<string, List<string>>();
        HashSet<string> dirCopyList = [];
        try
        {
            foreach (var src in srcList.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                var attributes = File.GetAttributes(src);
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    if (!src.EndsWith('\\'))
                        throw new ArgumentException($"The source path includes a non-existent file: {src}.");
                    dirCopyList.Add(src.TrimEnd('\\'));
                }
                else
                {
                    var dirPath = Path.GetDirectoryName(src) ??
                                  throw new ArgumentException($"{src} can't detect parent dir");
                    dirPath = dirPath.TrimEnd('\\');
                    if (!srcGroup.ContainsKey(dirPath))
                        srcGroup.Add(dirPath, []);

                    srcGroup[dirPath].Add(Path.GetFileName(src));
                }
            }
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, "Error", icon: MessageBoxImage.Error);
            return;
        }

        var needCreate = false;
        try
        {
            var destAttributes = File.GetAttributes(DestText);
            if (!destAttributes.HasFlag(FileAttributes.Directory))
                throw new ArgumentException($"The destination path is not a directory: {DestText}.");
        }
        catch (Exception e)
        {
            if (e is DirectoryNotFoundException or FileNotFoundException)
            {
                needCreate = true;
            }
            else
            {
                MessageBox.Show(e.Message, "Error", icon: MessageBoxImage.Error);
                return;
            }
        }

        if (needCreate)
            Directory.CreateDirectory(DestText);

        CopyLog = string.Empty;

        var roboCopy = new RoboCopy(
            output => CopyLog += $"{output}\n",
            error => CopyLog += $"Error : {error}\n"
        );

        var destDir = DestText.TrimEnd('\\');


        foreach (var dirCopy in dirCopyList)
        {
            var options = CreateDefaultBuilder().WithSubDirs(!ExcludeEmptyDirsOption).Build();
            await roboCopy.StartCopy(dirCopy, destDir, ["*.*"], options);
        }

        foreach (var group in srcGroup)
        {
            var dirPath = group.Key;
            var fileList = group.Value;
            var options = CreateDefaultBuilder().Build();
            await roboCopy.StartCopy(dirPath, destDir, fileList, options);
        }

        return;

        RoboCopyOptionsBuilder CreateDefaultBuilder()
        {
            var optionsBuilder = new RoboCopyOptionsBuilder();
            if (dryRun)
                optionsBuilder.DryRun();
            return optionsBuilder;
        }
    }

    [RelayCommand]
    private async Task DryRunCopy()
    {
        await Copy(true);
    }

    [RelayCommand]
    private async Task ExecuteCopy()
    {
        await Copy(false);
    }

    [RelayCommand]
    private void SelectDestDir()
    {
        var dialog = new FileOpenDialog(FileDialogFlag.PICK_FOLDER);
        if (!dialog.ShowDialog() || dialog.SelectedTargets.Count == 0)
            return;
        DestText = dialog.SelectedTargets[0];
    }

    [RelayCommand]
    private void SelectSources()
    {
        var dialog = new FileOpenDialog(FileDialogFlag.MULTI_SELECT);
        if (!dialog.ShowDialog())
            return;
        var appendBackslash = dialog.SelectedTargets.Select(src =>
        {
            try
            {
                var attributes = File.GetAttributes(src);
                if (attributes.HasFlag(FileAttributes.Directory))
                    src = src.TrimEnd('\\') + "\\";
                return src;
            }
            catch
            {
                // ignore
            }

            return "";
        }).Where(src => !string.IsNullOrWhiteSpace(src));
        SrcText = string.Join("\n", appendBackslash);
    }
}