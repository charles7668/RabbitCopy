using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using JetBrains.Annotations;
using RabbitCopy.Helper;

namespace RabbitCopy.Services;

/// <summary>
/// Windows taskbar integration.
/// </summary>
/// <remarks>
/// On Windows 11 the taskbar button icon of an installed application is taken from the
/// application (its shortcut or executable) instead of the window, so a runtime
/// <c>WM_SETICON</c> change is not reflected on a pinned button. Progress state and the
/// overlay icon are the only indicators that work in every taskbar state.
/// </remarks>
public sealed class TaskbarControlService : IDisposable
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

    /// <summary>
    /// The HICON currently shown as the overlay. The taskbar reads it asynchronously, so it
    /// must stay alive until it is replaced or the service is disposed.
    /// </summary>
    private HICON _overlayIcon;

    [UsedImplicitly]
    public void SetTaskbarProgress(Window window, TBPFLAG flag)
    {
        var hwnd = GetWindowHandle(window);
        if (hwnd.IsNull)
            return;

        _taskbarListInstance.SetProgressState(hwnd, flag);
    }

    /// <summary>
    /// Sets the badge drawn on the bottom-right corner of the taskbar button, or clears it
    /// when <paramref name="iconBytes"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="window">The window whose taskbar button should be updated.</param>
    /// <param name="iconBytes">A small (16x16) PNG image.</param>
    /// <param name="description">Accessibility description of the badge.</param>
    [UsedImplicitly]
    public void SetTaskbarOverlayIcon(Window window, byte[]? iconBytes, string? description)
    {
        var hwnd = GetWindowHandle(window);
        if (hwnd.IsNull)
            return;

        if (iconBytes is null || iconBytes.Length == 0)
        {
            SetOverlayIcon(hwnd, default, null);
            DestroyCurrentOverlayIcon();
            return;
        }

        var hIcon = CreateIcon(iconBytes);
        SetOverlayIcon(hwnd, hIcon, description);

        // Only release the previous icon after the new one has been handed to the taskbar.
        DestroyCurrentOverlayIcon();
        _overlayIcon = hIcon;
    }

    private unsafe void SetOverlayIcon(HWND hwnd, HICON icon, string? description)
    {
        fixed (char* descriptionPtr = description)
        {
            _taskbarListInstance.SetOverlayIcon(hwnd, icon, new PCWSTR(descriptionPtr));
        }
    }

    private static HWND GetWindowHandle(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        return handle == IntPtr.Zero ? default : new HWND(handle);
    }

    private static HICON CreateIcon(byte[] imageBytes)
    {
        using var stream = new MemoryStream(imageBytes);
        using var bitmap = new Bitmap(stream);
        return new HICON(bitmap.GetHicon());
    }

    private void DestroyCurrentOverlayIcon()
    {
        if (_overlayIcon.IsNull)
            return;

        PInvoke.DestroyIcon(_overlayIcon);
        _overlayIcon = default;
    }

    public void Dispose()
    {
        DestroyCurrentOverlayIcon();
    }
}
