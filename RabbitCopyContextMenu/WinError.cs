using Windows.Win32.Foundation;

namespace RabbitCopyContextMenu;

internal static class WinError
{
    public const int E_FAIL = -2147467259;
    public const uint SEVERITY_SUCCESS = 0;

    public static HRESULT MAKE_HRESULT(uint sev, uint fac, uint code)
    {
        return (HRESULT)((sev << 31) | (fac << 16) | code);
    }
}