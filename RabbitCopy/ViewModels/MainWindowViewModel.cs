using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using RabbitCopy.Enums;
using RabbitCopy.Helper;
using RabbitCopy.Models;
using RabbitCopy.RoboCopyModule;
using RabbitCopy.Services;
using MessageBox = HandyControl.Controls.MessageBox;

namespace RabbitCopy.ViewModels;

[method: UsedImplicitly]
public partial class ConfigIdentityChildCommandViewModel(string text, Action<ConfigIdentityViewModel>? execute)
    : ObservableObject
{
    [ObservableProperty]
    private string _text = text;

    [RelayCommand]
    private void Execute(ConfigIdentityViewModel vm)
    {
        execute?.Invoke(vm);
    }
}

public partial class ConfigIdentityViewModel : ObservableObject
{
    [ObservableProperty]
    private List<ConfigIdentityChildCommandViewModel> _childCommandViewModels = [];

    [ObservableProperty]
    private string _guid = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;
}

public partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(MainWindow window, RunOptions runOptions) : this(window)
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
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version!;
        var elevateService = App.ServiceProvider.GetRequiredService<ElevateService>();
        _elevateVisibility = elevateService.CanElevate() ? Visibility.Visible : Visibility.Hidden;
        _windowTitle = $"RabbitCopy v{version}";
        _iconUpdater = App.ServiceProvider.GetRequiredService<IconUpdater>();
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

        _windowIcon = ImageHelper.ByteArrayToBitmapImage(IconResource.rabbit_32x32);

        var historyService = App.ServiceProvider.GetRequiredService<HistoryService>();
        if (_runOptions?.OpenUI != false)
        {
            historyService.LoadHistory(out var srcHistories, out var dstHistories);
            _srcHistory = new ObservableCollection<string>(srcHistories);
            _dstHistory = new ObservableCollection<string>(dstHistories);
        }
    }

    private readonly IconUpdater _iconUpdater;

    private readonly RunOptions? _runOptions;

    private readonly MainWindow? _window;

    [ObservableProperty]
    private FileAttributes _addFileAttributes;

    [ObservableProperty]
    private ObservableCollection<ConfigIdentityViewModel> _configIdentities = [];

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
    private ObservableCollection<string> _dstHistory = [];

    [ObservableProperty]
    private Visibility _elevateVisibility;

    [ObservableProperty]
    private bool _enableFilterFileAttributes;

    [ObservableProperty]
    private bool _enableFilterName;

    [ObservableProperty]
    private bool _enableThrottling;

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
    private bool _noProgress;

    private float _progress;

    [ObservableProperty]
    private string _progressText = "0.0%";

    [ObservableProperty]
    private FileAttributes _removeFileAttributes;

    [ObservableProperty]
    private CopyModeItem _selectedCopyMode;

    [ObservableProperty]
    private string _selectedIoMaxSizeThrottlingUnit = "k";

    [ObservableProperty]
    private string _selectedIoRateThrottlingUnit = "k";

    [ObservableProperty]
    private string _selectedThresholdThrottlingUnit = "k";

    [ObservableProperty]
    private ObservableCollection<string> _srcHistory = [];

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

    [ObservableProperty]
    private string _windowTitle;

    private async Task Copy(bool dryRun)
    {
        if (string.IsNullOrWhiteSpace(SrcText) || string.IsNullOrWhiteSpace(DestText))
        {
            MessageBox.Show("Please select sources and destination", "Error", icon: MessageBoxImage.Error);
            return;
        }

        List<string> srcList = SrcText.Replace("\r", "").Split('\n').ToList();
        var srcGroup = new Dictionary<string, List<string>>();
        HashSet<string> dirCopyList = [];
        try
        {
            foreach (var src in srcList.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                var attributes = File.GetAttributes(src);
                if (attributes.HasFlag(FileAttributes.Directory))
                {
                    dirCopyList.Add(src.TrimEnd('\\').TrimEnd('/'));
                }
                else
                {
                    var dirPath = Path.GetDirectoryName(src) ??
                                  throw new ArgumentException($"{src} can't detect parent dir");
                    dirPath = dirPath.TrimEnd('\\').TrimEnd('/');
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

        UpdateWindowIcon(State.RUNNING);

        if (needCreate)
            Directory.CreateDirectory(DestText);

        CopyLog = string.Empty;

        // If the destination ends with a '\' or '/' , copy the contents into a new folder that has the same name as the source folder.
        var inNewFolder = DestText.EndsWith('\\') || DestText.EndsWith('/');

        var destDir = DestText.TrimEnd('\\').TrimEnd('/');

        _progress = 0.0f;
        var totalTaskCount = dirCopyList.Count + srcGroup.Count;
        var completeTaskCount = 0;
        var unitProgress = 100.0f / totalTaskCount;
        var errorExist = false;

        _copyProCancellationTokenSource = new CancellationTokenSource();

        var roboCopy = new RoboCopy(
            OnOutputReceive,
            OnErrorReceive
        );

        var cancellationToken = _copyProCancellationTokenSource.Token;

        await Task.Factory.StartNew(async () =>
        {
            foreach (var dirCopy in dirCopyList)
            {
                var destDirPath = destDir;
                if (inNewFolder)
                    destDirPath = Path.Join(destDirPath, Path.GetFileName(dirCopy));
                if (cancellationToken.IsCancellationRequested)
                    return;
                var options = CreateDefaultBuilder().WithSubDirs(!ExcludeEmptyDirsOption).Build();
                var filterNames = FilterName.Split(';');
                if (string.IsNullOrWhiteSpace(FilterName) || !EnableFilterName)
                    filterNames = ["*.*"];
                else
                    filterNames = filterNames.Select(x => "\"" + x + "\"").ToArray();
                await roboCopy.StartCopy(dirCopy, destDirPath, filterNames, options, cancellationToken);
                OnProgressUpdate(100);
                completeTaskCount++;
            }

            foreach (KeyValuePair<string, List<string>> group in srcGroup)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
                var dirPath = group.Key;
                List<string> fileList = group.Value;
                var options = CreateDefaultBuilder().Build();
                await roboCopy.StartCopy(dirPath, destDir, fileList, options, cancellationToken);
                OnProgressUpdate(100);
                completeTaskCount++;
            }
        }, TaskCreationOptions.LongRunning).Unwrap();

        UpdateWindowIcon(errorExist ? State.ERROR : State.SUCCESS);

        var historyService = App.ServiceProvider.GetRequiredService<HistoryService>();
        historyService.UpdateHistory(srcList, [DestText]);

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
                    if (float.TryParse(match.Groups[1].Value, out var progress))
                        OnProgressUpdate(progress);

                return;
            }

            CopyLog += $"{output}\n";
            WeakReferenceMessenger.Default.Send<ScrollToEndRequestMessage>();
        }

        void OnErrorReceive(string error)
        {
            CopyLog += $"Error : {error}\n";
            errorExist = true;
            WeakReferenceMessenger.Default.Send<ScrollToEndRequestMessage>();
        }

        RoboCopyOptionsBuilder CreateDefaultBuilder()
        {
            var optionsBuilder = new RoboCopyOptionsBuilder();
            if (dryRun)
                optionsBuilder.DryRun();
            optionsBuilder.SetCopyMode(SelectedCopyMode.Mode)
                .SetFileProperty(FileProperty).SetFileAttributes(AddFileAttributes, RemoveFileAttributes)
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

            if (NoProgress)
                optionsBuilder.DisableLogProgress();

            return optionsBuilder;
        }
    }

    [RelayCommand]
    private void DeleteConfig(ConfigIdentityViewModel identityViewModel)
    {
        var configService = App.ServiceProvider.GetRequiredService<ConfigService>();
        try
        {
            configService.RemoveConfig(new ConfigIdentity
            {
                Name = identityViewModel.Name,
                Guid = identityViewModel.Guid
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to delete config file: {ex.Message}", "Error", icon: MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DryRunCopy()
    {
        await Copy(true);
    }

    [RelayCommand]
    private void Elevate()
    {
        var elevateService = App.ServiceProvider.GetRequiredService<ElevateService>();
        if (!elevateService.CanElevate())
        {
            MessageBox.Show("Can't launch elevated process",
                "Error", icon: MessageBoxImage.Error);
            return;
        }

        WeakReferenceMessenger.Default.Send<ElevateRequestMessage>();
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
    private void LoadConfig(ConfigIdentityViewModel identityVm)
    {
        var configService = App.ServiceProvider.GetRequiredService<ConfigService>();
        try
        {
            var config = configService.LoadConfig(new ConfigIdentity
            {
                Name = identityVm.Name,
                Guid = identityVm.Guid
            });
            LoadFromConfig(config);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load config file: {ex.Message}", "Error", icon: MessageBoxImage.Error);
        }
    }

    private void LoadFromConfig(Config config)
    {
#pragma warning disable MVVMTK0034
        _createOnly = config.CreateOnly;
        _enableFilterFileAttributes = config.EnableFilterFileAttributes;
        _enableFilterName = config.EnableFilterName;
        _enableThrottling = config.EnableThrottling;
        _removeFileAttributes = config.RemoveFileAttributes;
        _excludeEmptyDirsOption = config.ExcludeEmptyDirsOption;
        _fileProperty = config.FileProperty;
        _filterFileAttributes = config.FilterFileAttributes;
        _filterName = config.FilterName;
        _addFileAttributes = config.AddFileAttributes;
        _selectedCopyMode = CopyModeItems.First(x => x == config.SelectedCopyMode);
        _selectedIoMaxSizeThrottlingUnit = config.SelectedIoMaxSizeThrottlingUnit;
        _selectedIoRateThrottlingUnit = config.SelectedIoRateThrottlingUnit;
        _selectedThresholdThrottlingUnit = config.SelectedThresholdThrottlingUnit;
        _threadNum = config.ThreadNum;
        _throttlingIoMaxSize = config.ThrottlingIoMaxSize;
        _throttlingIoRate = config.ThrottlingIoRate;
        _throttlingThreshold = config.ThrottlingThreshold;
        _unbufferedIo = config.UnbufferedIo;
#pragma warning restore MVVMTK0034
        OnPropertyChanged(string.Empty);
    }

    [RelayCommand]
    private void OpenFileAttributesSelectWindowExclude()
    {
        var vm = new FileAttributesSelectWindowViewModel(RemoveFileAttributes, true);
        var window = new FileAttributesSelectWindow(vm)
        {
            Owner = _window
        };
        window.ShowDialog();
        RemoveFileAttributes = GetSelectedFileAttributes(vm);
    }

    [RelayCommand]
    private void OpenFileAttributesSelectWindowInclude()
    {
        var vm = new FileAttributesSelectWindowViewModel(AddFileAttributes, false);
        var window = new FileAttributesSelectWindow(vm)
        {
            Owner = _window
        };
        window.ShowDialog();
        AddFileAttributes = GetSelectedFileAttributes(vm);
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
    private void OpenShellExtensionInstaller()
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "RabbitCopyInstaller.exe"
        };
        try
        {
            Process.Start(startInfo);
        }
        catch (Exception e)
        {
            MessageBox.Show($"Failed to open installer: {e.Message}", "Error", icon: MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void SaveConfig()
    {
        var configService = App.ServiceProvider.GetRequiredService<ConfigService>();
        var appPathService = App.ServiceProvider.GetRequiredService<AppPathService>();
        InputBoxWindowViewModel vm = new()
        {
            Title = "Please enter a name for the config file",
            Text = ""
        };
        vm.OnValidate += _ =>
        {
            if (string.IsNullOrWhiteSpace(vm.Text))
            {
                MessageBox.Show("Config name cannot be empty",
                    "Error", icon: MessageBoxImage.Error);
                return false;
            }

            if (!configService.IsConfigExist(vm.Text))
                return true;
            MessageBox.Show("Config file with the same name already exists. Please choose a different name.",
                "Error", icon: MessageBoxImage.Error);
            return false;
        };
        InputBoxWindow win = new(vm)
        {
            Owner = _window,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        if (win.ShowDialog() != true)
            return;
        var configListFile = appPathService.ConfigIdentityListFile;
        var backupConfigListFile = "bak-" + configListFile;
        try
        {
            if (File.Exists(configListFile))
                File.Copy(configListFile, backupConfigListFile, true);
            configService.SaveNewConfig(vm.Text, ToConfig());
        }
        catch (Exception e)
        {
            if (File.Exists(backupConfigListFile))
                File.Move(backupConfigListFile, configListFile, true);
            MessageBox.Show($"Failed to save config file: {e.Message}", "Error", icon: MessageBoxImage.Error);
            return;
        }
        finally
        {
            if (File.Exists(backupConfigListFile))
                File.Delete(backupConfigListFile);
        }

        MessageBox.Show("Config file saved successfully", "Info", icon: MessageBoxImage.Information);
    }

    [RelayCommand]
    private void SelectDestDir()
    {
        var dialog = new FileOpenDialog(FileDialogFlag.PICK_FOLDER);
        if (!dialog.ShowDialog() || dialog.SelectedTargets.Count == 0)
            return;
        DestText = dialog.SelectedTargets[0] + "\\";
    }

    [RelayCommand]
    private void SelectDstHistory(string history)
    {
        DestText = history;
    }

    [RelayCommand]
    private void SelectSources()
    {
        var dialog = new FileOpenDialog(FileDialogFlag.MULTI_SELECT);
        if (!dialog.ShowDialog())
            return;
        IEnumerable<string> appendBackslash = dialog.SelectedTargets.Select(src =>
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

    [RelayCommand]
    private void SelectSrcHistory(string history)
    {
        SrcText = history;
    }

    [RelayCommand]
    private void SettingsSubmenuClosed(MenuItem menuItem)
    {
        if (menuItem.IsSubmenuOpen)
            return;
        ConfigIdentities = [];
    }

    [RelayCommand]
    private void SettingsSubmenuOpened()
    {
        if (ConfigIdentities.Count > 0)
            return;
        var configService = App.ServiceProvider.GetRequiredService<ConfigService>();
        List<ConfigIdentity> identities = configService.LoadConfigIdentityList();
        List<ConfigIdentityViewModel> vms = identities.Select(identity => new ConfigIdentityViewModel
        {
            Name = identity.Name, Guid = identity.Guid, ChildCommandViewModels =
            [
                new ConfigIdentityChildCommandViewModel("load", LoadConfig),
                new ConfigIdentityChildCommandViewModel("delete", DeleteConfig)
            ]
        }).ToList();

        ConfigIdentities = new ObservableCollection<ConfigIdentityViewModel>(vms);
    }

    private void ShutDown()
    {
        WeakReferenceMessenger.Default.Send(new ShutdownRequestMessage());
    }

    private Config ToConfig()
    {
        return new Config
        {
            CreateOnly = CreateOnly,
            EnableFilterFileAttributes = EnableFilterFileAttributes,
            EnableFilterName = EnableFilterName,
            EnableThrottling = EnableThrottling,
            RemoveFileAttributes = RemoveFileAttributes,
            ExcludeEmptyDirsOption = ExcludeEmptyDirsOption,
            FileProperty = FileProperty,
            FilterFileAttributes = FilterFileAttributes,
            FilterName = FilterName,
            AddFileAttributes = AddFileAttributes,
            SelectedCopyMode = SelectedCopyMode,
            SelectedIoMaxSizeThrottlingUnit = SelectedIoMaxSizeThrottlingUnit,
            SelectedIoRateThrottlingUnit = SelectedIoRateThrottlingUnit,
            SelectedThresholdThrottlingUnit = SelectedThresholdThrottlingUnit,
            ThreadNum = ThreadNum,
            ThrottlingIoMaxSize = ThrottlingIoMaxSize,
            ThrottlingIoRate = ThrottlingIoRate,
            ThrottlingThreshold = ThrottlingThreshold,
            UnbufferedIo = UnbufferedIo
        };
    }

    private void UpdateWindowIcon(State state)
    {
        if (_window is null)
            return;
        switch (state)
        {
            case State.IDLE:
                UpdateIcon(IconResource.rabbit_32x32);
                break;
            case State.RUNNING:
                UpdateIcon(IconResource.rabbit_yellow_32x32);
                break;
            case State.SUCCESS:
                UpdateIcon(IconResource.rabbit_green_32x32);
                break;
            case State.ERROR:
                UpdateIcon(IconResource.rabbit_red_32x32);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }

        void UpdateIcon(byte[] imageData)
        {
            _iconUpdater.UpdateTaskbarIcon(_window!, imageData);
        }
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
            if (_runOptions.Guid != null)
            {
                var configService = App.ServiceProvider.GetRequiredService<ConfigService>();
                var configIdentities = configService.LoadConfigIdentityList();

                var targetConfigIdentity = configIdentities.FirstOrDefault(x => x.Guid == _runOptions.Guid);
                if (targetConfigIdentity == null)
                {
                    MessageBox.Show($"The specified config GUID {_runOptions.Guid} does not exist.",
                        "Error", icon: MessageBoxImage.Error);
                    ShutDown();
                    return;
                }

                var targetConfig = configService.LoadConfig(targetConfigIdentity);
                LoadFromConfig(targetConfig);
            }

            await ExecuteCopy();
            ShutDown();
        }
    }
}