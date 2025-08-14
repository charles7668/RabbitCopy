using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MS.WindowsAPICodePack.Internal;

namespace RabbitCopyContextMenu
{
    internal static class WinError
    {
        public const int E_FAIL = -2147467259;
        public const uint SEVERITY_SUCCESS = 0;

        public static HResult MAKE_HRESULT(uint sev, uint fac, uint code)
        {
            return (HResult)((sev << 31) | (fac << 16) | code);
        }
    }
}