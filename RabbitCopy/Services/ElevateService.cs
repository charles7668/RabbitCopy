using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;

namespace RabbitCopy.Services;

public class SafeHandleWrapper(IntPtr invalidHandleValue) : SafeHandle(invalidHandleValue, true)
{
    public override bool IsInvalid => false;

    protected override bool ReleaseHandle()
    {
        SetHandle(IntPtr.Zero);
        return true;
    }
}

public class ElevateService
{
    public unsafe bool CanElevate()
    {
        var hToken = HANDLE.Null;
        var tokenType = TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited;

        try
        {
            if (!PInvoke.OpenProcessToken(
                    (HANDLE)Process.GetCurrentProcess().Handle,
                    TOKEN_ACCESS_MASK.TOKEN_ALL_ACCESS,
                    &hToken))
                return false;

            using var safeHandle = new SafeHandleWrapper((IntPtr)hToken.Value);
            PInvoke.GetTokenInformation(
                safeHandle,
                TOKEN_INFORMATION_CLASS.TokenElevationType,
                &tokenType,
                sizeof(TOKEN_ELEVATION_TYPE),
                out var _);

            return tokenType == TOKEN_ELEVATION_TYPE.TokenElevationTypeLimited;
        }
        finally
        {
            if (hToken != HANDLE.Null)
                PInvoke.CloseHandle(hToken);
        }
    }
}