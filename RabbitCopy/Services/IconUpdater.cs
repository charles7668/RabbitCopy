using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using JetBrains.Annotations;

namespace RabbitCopy.Services;

public class IconUpdater
{
    private const uint WM_SETICON = 0x0080;
    private const int ICON_BIG = 1;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// Update Taskbar Icon
    /// </summary>
    /// <param name="window">update icon window</param>
    /// <param name="newBitmapBytes">icon bitmap</param>
    [UsedImplicitly]
    public void UpdateTaskbarIcon(Window window, byte[] newBitmapBytes)
    {
        var helper = new WindowInteropHelper(window);
        var hwnd = helper.Handle;

        using var ms = new MemoryStream(newBitmapBytes);
        using var newBitmap = new Bitmap(ms);
        var hIcon = newBitmap.GetHicon();
        window.Icon = Imaging.CreateBitmapSourceFromHIcon(
            hIcon,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());

        SendMessage(hwnd, WM_SETICON, ICON_BIG, hIcon);
    }
}