using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using JetBrains.Annotations;
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

    [UsedImplicitly]
    public void SetTaskbarProgress(Window window, TBPFLAG flag)
    {
        var windowPtr = new WindowInteropHelper(window).Handle;
        var hwnd = new HWND(windowPtr);
        _taskbarListInstance.SetProgressState(hwnd, flag);
    }

    [UsedImplicitly]
    public void UpdateTaskbarIcon(Window window, byte[] newBitmapBytes)
    {
        var helper = new WindowInteropHelper(window);
        var hwnd = new HWND(helper.Handle);

        using var ms = new MemoryStream(newBitmapBytes);
        using var newBitmap = new Bitmap(ms);
        var hIcon = newBitmap.GetHicon();
        window.Icon = Imaging.CreateBitmapSourceFromHIcon(
            hIcon,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        PInvoke.SendMessage(hwnd, PInvoke.WM_SETICON, PInvoke.ICON_BIG, hIcon);
    }
}