using CommunityToolkit.Mvvm.Messaging;
using JetBrains.Annotations;
using RabbitCopy.Models;
using RabbitCopy.ViewModels;

namespace RabbitCopy;

/// <summary>
/// InputBoxWindow.xaml 的互動邏輯
/// </summary>
public partial class InputBoxWindow
{
    [UsedImplicitly]
    public InputBoxWindow(InputBoxWindowViewModel vm)
    {
        InitializeComponent();
        _guid = Guid.NewGuid();
        vm.MessageChannel = _guid.ToString();
        RegisterMessageReceiver();
        DataContext = vm;
    }

    [UsedImplicitly]
    public InputBoxWindow() : this(new InputBoxWindowViewModel())
    {
    }

    private readonly Guid _guid;

    private void RegisterMessageReceiver()
    {
        WeakReferenceMessenger.Default.Register<CloseWindowRequestMessage, string>(this,
            _guid.ToString(), (_, msg) => Dispatcher.Invoke(() =>
            {
                DialogResult = msg.DialogResult;
                Close();
            }));
    }
}