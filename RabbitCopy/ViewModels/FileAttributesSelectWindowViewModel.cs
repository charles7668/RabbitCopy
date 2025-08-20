using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;

namespace RabbitCopy.ViewModels;

public partial class FileAttributesSelectWindowViewModel : ObservableObject
{
    [UsedImplicitly]
    public FileAttributesSelectWindowViewModel()
    {
    }

    public FileAttributesSelectWindowViewModel(FileAttributes attributes, bool excludeMode)
    {
        _readOnly = attributes.HasFlag(FileAttributes.ReadOnly);
        _archive = attributes.HasFlag(FileAttributes.Archive);
        _compressed = attributes.HasFlag(FileAttributes.Compressed);
        _encrypted = attributes.HasFlag(FileAttributes.Encrypted);
        _system = attributes.HasFlag(FileAttributes.System);
        _hidden = attributes.HasFlag(FileAttributes.Hidden);
        _notContentIndexed = attributes.HasFlag(FileAttributes.NotContentIndexed);
        _temporary = attributes.HasFlag(FileAttributes.Temporary);
        _offline = attributes.HasFlag(FileAttributes.Offline);
        if (excludeMode)
        {
            _offlineVisibility = Visibility.Visible;
            _offline = false;
        }
    }

    [ObservableProperty]
    private bool _archive;

    [ObservableProperty]
    private bool _compressed;

    [ObservableProperty]
    private bool _encrypted;

    [ObservableProperty]
    private bool _hidden;

    [ObservableProperty]
    private bool _notContentIndexed;

    // offline property only shown when excludeMode is true
    [ObservableProperty]
    private bool _offline;

    [ObservableProperty]
    private Visibility _offlineVisibility = Visibility.Hidden;

    [ObservableProperty]
    private bool _readOnly;

    [ObservableProperty]
    private bool _system;

    [ObservableProperty]
    private bool _temporary;
}