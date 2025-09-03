using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace RabbitCopy.Helper;

internal static class Win32Helper
{
    internal static class User32
    {
        [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern nint GetWindowLongPtr(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex);

        [DllImport("USER32.dll", ExactSpelling = true, EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern nint SetWindowLongPtr(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, nint dwNewLong);
    }
}