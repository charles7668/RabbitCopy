using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using RabbitCopy.Helper;

namespace RabbitCopy.Services;

public class TaskbarControlService
{
    public TaskbarControlService()
    {
        Guid clsidTaskbarList = new(COMHelper.CLSID.TASKBAR_LIST);
        Guid iidTaskbarList = new(COMHelper.IID.TASKBAR_LIST);
        PInvoke.CoCreateInstance(clsidTaskbarList, null, CLSCTX.CLSCTX_INPROC_SERVER,
            iidTaskbarList,
            out var comObject);
        _taskbarListInstance = (ITaskbarList3)comObject!;
    }

    private readonly ITaskbarList3 _taskbarListInstance;

    public void SetTaskbarProgress(Window window, TBPFLAG flag)
    {
        var windowPtr = new WindowInteropHelper(window).Handle;
        var hwnd = new HWND(windowPtr);
        _taskbarListInstance.SetProgressState(hwnd, flag);
    }
}