using CommunityToolkit.Mvvm.ComponentModel;
using RabbitCopy.Enums;

namespace RabbitCopy.ViewModels;

public partial class FilePropertySelectWindowViewModel : ObservableObject
{
    public FilePropertySelectWindowViewModel()
    {
    }

    public FilePropertySelectWindowViewModel(FileProperty fileProperty)
    {
        _data = fileProperty.HasFlag(FileProperty.DATA);
        _attributes = fileProperty.HasFlag(FileProperty.ATTRIBUTES);
        _timeStamp = fileProperty.HasFlag(FileProperty.TIME_STAMP);
        _altStream = fileProperty.HasFlag(FileProperty.ALT_STREAMS);
        _acl = fileProperty.HasFlag(FileProperty.ACL);
        _ownerInformation = fileProperty.HasFlag(FileProperty.OWNER_INFORMATION);
        _auditingInformation = fileProperty.HasFlag(FileProperty.AUDITING_INFORMATION);
    }

    [ObservableProperty]
    private bool _acl;

    [ObservableProperty]
    private bool _altStream;

    [ObservableProperty]
    private bool _attributes;

    [ObservableProperty]
    private bool _auditingInformation;

    [ObservableProperty]
    private bool _data;

    [ObservableProperty]
    private bool _ownerInformation;

    [ObservableProperty]
    private bool _timeStamp;
}