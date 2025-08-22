using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RabbitCopy.Models;

namespace RabbitCopy.ViewModels;

public partial class InputBoxWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _text = string.Empty;

    [ObservableProperty]
    private string _title = "InputBox";

    /// <summary>
    /// Message channel for communication with view.
    /// </summary>
    public string MessageChannel { private get; set; } = "";

    /// <summary>
    /// when user clicks OK button, this event will be raised.
    /// return false to cancel the operation.
    /// </summary>
    public event Func<InputBoxWindowViewModel, bool>? OnValidate;

    [RelayCommand]
    private void Cancel()
    {
        SendCloseWindowRequest(false);
    }

    [RelayCommand]
    private void Ok()
    {
        var res = OnValidate?.Invoke(this);
        if (res is false)
            return;
        SendCloseWindowRequest(true);
    }

    private void SendCloseWindowRequest(bool dialogResult)
    {
        WeakReferenceMessenger.Default.Send(new CloseWindowRequestMessage
        {
            DialogResult = dialogResult
        }, MessageChannel);
    }
}