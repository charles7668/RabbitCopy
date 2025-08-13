using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MessageBox = HandyControl.Controls.MessageBox;

namespace RabbitCopy.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _copyLog = string.Empty;

    [ObservableProperty]
    private string _destText = string.Empty;

    [ObservableProperty]
    private string _srcText = string.Empty;

    [RelayCommand]
    private Task ExecuteCopy()
    {
        if (string.IsNullOrWhiteSpace(SrcText) || string.IsNullOrWhiteSpace(DestText))
        {
            MessageBox.Show("Please select sources and destination", "Error", icon: MessageBoxImage.Error);
            return Task.CompletedTask;
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
            return Task.CompletedTask;
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
                return Task.CompletedTask;
            }
        }

        if (needCreate)
            Directory.CreateDirectory(DestText);

        CopyLog = string.Empty;

        var destDir = DestText.TrimEnd('\\');

        foreach (var dirCopy in dirCopyList)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "robocopy",
                Arguments = $"\"{dirCopy}\" \"{destDir}\" *.*",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = new Process
            {
                StartInfo = startInfo
            };
            proc.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    CopyLog += $"{args.Data}\n";
            };
            proc.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    CopyLog += $"Error : {args.Data}\n";
            };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
        }

        foreach (var group in srcGroup)
        {
            var dirPath = group.Key;
            var fileList = group.Value;
            var fileArgs = string.Join(" ", fileList.Select(f => $"\"{f}\""));
            ProcessStartInfo startInfo = new()
            {
                FileName = "robocopy",
                Arguments = $"\"{dirPath}\" \"{destDir}\" {fileArgs}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = new Process
            {
                StartInfo = startInfo
            };
            proc.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    CopyLog += $"{args.Data}\n";
            };
            proc.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                    CopyLog += $"Error : {args.Data}\n";
            };
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
        }

        return Task.CompletedTask;
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