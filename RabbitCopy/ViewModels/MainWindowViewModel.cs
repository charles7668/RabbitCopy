using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using MS.WindowsAPICodePack.Internal;

namespace RabbitCopy.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [RelayCommand]
    private void SelectSources()
    {
        var dialog = new FileOpenDialog();
        dialog.ShowDialog();
        MessageBox.Show(dialog.SelectedTargets.Count.ToString());
    }
}