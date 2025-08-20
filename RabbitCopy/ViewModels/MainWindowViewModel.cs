using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using RabbitCopy.Enums;
using RabbitCopy.Helper;
using RabbitCopy.Models;
using RabbitCopy.RoboCopyModule;
using MessageBox = HandyControl.Controls.MessageBox;

namespace RabbitCopy.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(RunOptions runOptions) : this()
    {
        _runOptions = runOptions;
        _srcText = string.Join("\n", runOptions.SrcPaths ?? []);
        _destText = runOptions.DestPath ?? string.Empty;
    }

    public MainWindowViewModel(MainWindow window) : this()
    {
        _window = window;
    }

    [UsedImplicitly]
    public MainWindowViewModel()
    {
        _copyModeItems =
        [
            new CopyModeItem
            {
                Mode = CopyMode.DIFF_NO_OVERWRITE, Description = "Only copy files that don't exist",
                Category = "Group1"
            },

            new CopyModeItem
            {
                Mode = CopyMode.DIFF_SIZE_DATE, Description = "Copy files with different size or date",
                Category = "Group1"
            },

            new CopyModeItem
                { Mode = CopyMode.DIFF_NEWER, Description = "Copy only newer files", Category = "Group1" },
            new CopyModeItem
                { Mode = CopyMode.COPY_OVERWRITE, Description = "Copy all files with overwrite", Category = "Group1" },
            // Group2
            new CopyModeItem
            {
                Mode = CopyMode.SYNC_SIZE_DATE, Description = "Sync files with different size or date",
                Category = "Group2"
            },
            new CopyModeItem
                { Mode = CopyMode.MOVE_OVERWRITE, Description = "Move files with overwrite", Category = "Group2" },
            new CopyModeItem
            {
                Mode = CopyMode.MOVE_NO_OVERWRITE, Description = "Move files that don't exist in destination",
                Category = "Group2"
            }
        ];

        _copyModeView = CollectionViewSource.GetDefaultView(_copyModeItems);
        _copyModeView.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

        _selectedCopyMode = _copyModeItems[1];

        WindowIcon = ImageHelper.ByteArrayToBitmapImage(IconResource.rabbit_32x32);
    }

    private readonly RunOptions? _runOptions;

    private readonly MainWindow? _window;

    [ObservableProperty]
    private string _copyLog = string.Empty;

    [ObservableProperty]
    private ObservableCollection<CopyModeItem> _copyModeItems;

    [ObservableProperty]
    private ICollectionView _copyModeView;

    private CancellationTokenSource _copyProCancellationTokenSource = new();

    [ObservableProperty]
    private bool _createOnly;

    [ObservableProperty]
    private string _destText = string.Empty;

    [ObservableProperty]
    private bool _enableFilterFileAttributes;

    [ObservableProperty]
    private bool _enableFilterName;

    [ObservableProperty]
    private bool _enableThrottling;

    [ObservableProperty]
    private FileAttributes _excFileAttributes;

    [ObservableProperty]
    private bool _excludeEmptyDirsOption;

    [ObservableProperty]
    private FileProperty _fileProperty = FileProperty.DATA | FileProperty.ATTRIBUTES |
                                         FileProperty.TIME_STAMP;

    [ObservableProperty]
    private FileAttributes _filterFileAttributes;

    [ObservableProperty]
    private string _filterName = string.Empty;

    [ObservableProperty]
    private FileAttributes _incFileAttributes;

    private float _progress;

    [ObservableProperty]
    private string _progressText = "0.0%";

    [ObservableProperty]
    private CopyModeItem _selectedCopyMode;

    [ObservableProperty]
    private string _selectedIoMaxSizeThrottlingUnit = "k";

    [ObservableProperty]
    private string _selectedIoRateThrottlingUnit = "k";

    [ObservableProperty]
    private string _selectedThresholdThrottlingUnit = "k";

    [ObservableProperty]
    private string _srcText = string.Empty;

    [ObservableProperty]
    private uint _threadNum = 8;

    [ObservableProperty]
    private uint _throttlingIoMaxSize;

    [ObservableProperty]
    private uint _throttlingIoRate;

    [ObservableProperty]
    private uint _throttlingThreshold;

    [ObservableProperty]
    private ObservableCollection<string> _throttlingUnits = ["k", "m", "g"];

    [ObservableProperty]
    private bool _unbufferedIo;

    [ObservableProperty]
    private BitmapImage? _windowIcon;

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
                needCreate = true;
            else
            {
                MessageBox.Show(e.Message, "Error", icon: MessageBoxImage.Error);
                return;
            }
        }

        if (needCreate)
            Directory.CreateDirectory(DestText);

        CopyLog = string.Empty;

        var destDir = DestText.TrimEnd('\\');

        _progress = 0.0f;
        var totalTaskCount = dirCopyList.Count + srcGroup.Count;
        var completeTaskCount = 0;
        var unitProgress = 100.0f / totalTaskCount;

        _copyProCancellationTokenSource = new CancellationTokenSource();

        var roboCopy = new RoboCopy(
            OnOutputReceive,
            error => CopyLog += $"Error : {error}\n"
        );

        var cancellationToken = _copyProCancellationTokenSource.Token;

        await Task.Factory.StartNew(async () =>
        {
            foreach (var dirCopy in dirCopyList)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                var options = CreateDefaultBuilder().WithSubDirs(!ExcludeEmptyDirsOption).Build();
                var filterNames = FilterName.Split(';');
                if (string.IsNullOrWhiteSpace(FilterName) || !EnableFilterName)
                    filterNames = ["*.*"];
                else
                    filterNames = filterNames.Select(x => "\"" + x + "\"").ToArray();
                await roboCopy.StartCopy(dirCopy, destDir, filterNames, options, cancellationToken);
                OnProgressUpdate(100);
                completeTaskCount++;
            }

            foreach (var group in srcGroup)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                var dirPath = group.Key;
                var fileList = group.Value;
                var options = CreateDefaultBuilder().Build();
                await roboCopy.StartCopy(dirPath, destDir, fileList, options, cancellationToken);
                OnProgressUpdate(100);
                completeTaskCount++;
            }
        }, TaskCreationOptions.LongRunning).Unwrap();

        WeakReferenceMessenger.Default.Send<ScrollToEndRequestMessage>();

        return;

        void OnProgressUpdate(float progress)
        {
            _progress = unitProgress * completeTaskCount + progress / 100 * unitProgress;
            ProgressText = $"{_progress:0.0}%";
        }

        void OnOutputReceive(string output)
        {
            if (output.Length == 6)
            {
                var pattern = new Regex("^\\s?(\\d+.\\d)%");
                var match = pattern.Match(output);
                if (match.Success)
                {
                    if (float.TryParse(match.Groups[1].Value, out var progress))
                        OnProgressUpdate(progress);
                }

                return;
            }

            CopyLog += $"{output}\n";
            WeakReferenceMessenger.Default.Send<ScrollToEndRequestMessage>();
        }

        RoboCopyOptionsBuilder CreateDefaultBuilder()
        {
            var optionsBuilder = new RoboCopyOptionsBuilder();
            if (dryRun)
                optionsBuilder.DryRun();
            optionsBuilder.SetCopyMode(SelectedCopyMode.Mode)
                .SetFileProperty(FileProperty).SetFileAttributes(IncFileAttributes, ExcFileAttributes)
                .SetThreadNum(ThreadNum);
            if (UnbufferedIo)
                optionsBuilder.EnableUnbufferedIo();
            if (CreateOnly)
                optionsBuilder.CreateOnly();
            if (EnableThrottling)
            {
                string ioMaxSize = string.Empty, ioRate = string.Empty, threshold = string.Empty;
                if (ThrottlingIoMaxSize > 0)
                    ioMaxSize = $"{ThrottlingIoMaxSize}{SelectedIoMaxSizeThrottlingUnit}";
                if (ThrottlingIoRate > 0)
                    ioRate = $"{ThrottlingIoRate}{SelectedIoRateThrottlingUnit}";
                if (ThrottlingThreshold > 0)
                    threshold = $"{ThrottlingThreshold}{SelectedThresholdThrottlingUnit}";
                optionsBuilder.WithThrottling(ioMaxSize, ioRate, threshold);
            }

            if (EnableFilterFileAttributes)
                optionsBuilder.WithFileAttributesFilter(FilterFileAttributes);

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

    private FileAttributes GetSelectedFileAttributes(FileAttributesSelectWindowViewModel vm)
    {
        var attributes = FileAttributes.None;
        if (vm.ReadOnly)
            attributes |= FileAttributes.ReadOnly;
        if (vm.Archive)
            attributes |= FileAttributes.Archive;
        if (vm.System)
            attributes |= FileAttributes.System;
        if (vm.Hidden)
            attributes |= FileAttributes.Hidden;
        if (vm.Compressed)
            attributes |= FileAttributes.Compressed;
        if (vm.NotContentIndexed)
            attributes |= FileAttributes.NotContentIndexed;
        if (vm.Encrypted)
            attributes |= FileAttributes.Encrypted;
        if (vm.Temporary)
            attributes |= FileAttributes.Temporary;
        if (vm.Offline)
            attributes |= FileAttributes.Offline;
        return attributes;
    }

    [RelayCommand]
    private void OpenFileAttributesSelectWindowExclude()
    {
        var vm = new FileAttributesSelectWindowViewModel(ExcFileAttributes, true);
        var window = new FileAttributesSelectWindow(vm)
        {
            Owner = _window
        };
        window.ShowDialog();
        ExcFileAttributes = GetSelectedFileAttributes(vm);
    }

    [RelayCommand]
    private void OpenFileAttributesSelectWindowInclude()
    {
        var vm = new FileAttributesSelectWindowViewModel(IncFileAttributes, false);
        var window = new FileAttributesSelectWindow(vm)
        {
            Owner = _window
        };
        window.ShowDialog();
        IncFileAttributes = GetSelectedFileAttributes(vm);
    }

    [RelayCommand]
    private void OpenFilePropertySelectWindow()
    {
        var viewModel = new FilePropertySelectWindowViewModel(FileProperty);
        var window = new FilePropertySelectWindow(viewModel);
        if (_window != null)
            window.Owner = _window;
        window.ShowDialog();
        FileProperty newProperty = 0;
        if (viewModel.Data)
            newProperty |= FileProperty.DATA;
        if (viewModel.Attributes)
            newProperty |= FileProperty.ATTRIBUTES;
        if (viewModel.TimeStamp)
            newProperty |= FileProperty.TIME_STAMP;
        if (viewModel.AltStream)
            newProperty |= FileProperty.ALT_STREAMS;
        if (viewModel.Acl)
            newProperty |= FileProperty.ACL;
        if (viewModel.OwnerInformation)
            newProperty |= FileProperty.OWNER_INFORMATION;
        if (viewModel.AuditingInformation)
            newProperty |= FileProperty.AUDITING_INFORMATION;
        FileProperty = newProperty;
    }

    [RelayCommand]
    private void OpenFilterFileAttributesSelectWindow()
    {
        var vm = new FileAttributesSelectWindowViewModel(FilterFileAttributes, true);
        var window = new FileAttributesSelectWindow(vm)
        {
            Owner = _window
        };
        window.ShowDialog();
        FilterFileAttributes = GetSelectedFileAttributes(vm);
    }

    [RelayCommand]
    private void OpenFilterFileNameSettingWindow()
    {
        var initialText = string.Join('\n', FilterName.Split(';'));
        var vm = new FilterFileNameSettingWindowViewModel(initialText);
        var window = new FilterFileNameSettingWindow(vm)
        {
            Owner = _window
        };
        window.ShowDialog();
        var result = string.Join(";", vm.FilterItemText.Replace("\r", "").Split('\n'));
        FilterName = result;
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

    private void ShutDown()
    {
        WeakReferenceMessenger.Default.Send(new ShutdownRequestMessage());
    }

    [RelayCommand]
    private void WindowClosing()
    {
        _copyProCancellationTokenSource.Cancel();
    }

    [RelayCommand]
    private async Task WindowContentRendered()
    {
        // normal launch
        if (_runOptions is null)
            return;

        // if open ui is not specified, execute copy directly
        if (!_runOptions.OpenUI)
        {
            await ExecuteCopy();
            ShutDown();
        }
    }
}