using CommunityToolkit.Mvvm.ComponentModel;

namespace RabbitCopy.ViewModels;

public partial class FilterFileNameSettingWindowViewModel : ObservableObject
{
    public FilterFileNameSettingWindowViewModel()
    {
        _filterItemText = string.Empty;
    }

    public FilterFileNameSettingWindowViewModel(string initialText) : this()
    {
        _filterItemText = initialText;
    }

    [ObservableProperty]
    private string _filterItemText;
}