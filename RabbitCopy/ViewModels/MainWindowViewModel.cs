using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RabbitCopy.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _srcText = string.Empty;

    [ObservableProperty]
    private string _destText = string.Empty;

    [RelayCommand]
    private void SelectSources()
    {
        var dialog = new FileOpenDialog(FileDialogFlag.MULTI_SELECT);
        if (!dialog.ShowDialog())
            return;
        SrcText = string.Join("\n", dialog.SelectedTargets);
    }

    [RelayCommand]
    private void SelectDestDir()
    {
        var dialog = new FileOpenDialog(FileDialogFlag.PICK_FOLDER);
        if (!dialog.ShowDialog() || dialog.SelectedTargets.Count == 0)
            return;
        DestText = dialog.SelectedTargets[0];
    }
}