using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using MS.WindowsAPICodePack.Internal;
using static RabbitCopyContextMenu.ShellExt;

namespace RabbitCopyContextMenu;

[ClassInterface(ClassInterfaceType.None)]
[Guid("FE6B2057-2C0F-4461-9455-67116990409E")]
[ComVisible(true)]
public class ContextMenuExt : IShellExtInit, IContextMenu
{
    public ContextMenuExt()
    {
        using var ms = new MemoryStream(Resource.rabbit_16x16);
        try
        {
            var bitmap = new Bitmap(ms);
            _menuBitmap = bitmap.GetHbitmap();
        }
        catch
        {
            _menuBitmap = IntPtr.Zero;
        }
    }

    private readonly List<string> _dstArray = [];
    private readonly List<string> _srcArray = [];
    private IntPtr _menuBitmap;

    /// <summary>
    /// Queries the context menu for the specified menu handle and command range.
    /// </summary>
    /// <param name="hMenu"></param>
    /// <param name="iMenu"></param>
    /// <param name="idCmdFirst"></param>
    /// <param name="idCmdLast"></param>
    /// <param name="uFlags"></param>
    /// <returns></returns>
    public HResult QueryContextMenu(IntPtr hMenu, uint iMenu, uint idCmdFirst, uint idCmdLast, uint uFlags)
    {
        var cmfFlag = (CMF)uFlags;
        if (_srcArray.Count == 0 && _dstArray.Count == 0 && cmfFlag.HasFlag(CMF.CMF_NORMAL))
            return WinError.MAKE_HRESULT(WinError.SEVERITY_SUCCESS, 0, 0);

        if (cmfFlag.HasFlag(CMF.CMF_VERBSONLY | CMF.CMF_DEFAULTONLY))
            return WinError.MAKE_HRESULT(WinError.SEVERITY_SUCCESS, 0, 0);

        uint menuItemCount = 0;
        var sep = new MENUITEMINFO();
        sep.cbSize = (uint)Marshal.SizeOf(sep);
        sep.fMask = MIIM.MIIM_TYPE;
        sep.fType = MFT.MFT_SEPARATOR;
        if (!InsertMenuItem(hMenu, menuItemCount++, true, ref sep))
            return (HResult)Marshal.GetHRForLastWin32Error();
        if (_srcArray.Count > 0)
        {
            MENUITEMINFO menuItemInfo = new()
            {
                fMask = MIIM.MIIM_ID | MIIM.MIIM_STRING | MIIM.MIIM_FTYPE | MIIM.MIIM_BITMAP,
                wID = idCmdFirst,
                fType = MFT.MFT_STRING,
                dwTypeData = "Copy",
                cch = 11,
                cbSize = (uint)Marshal.SizeOf<MENUITEMINFO>(),
                hbmpItem = _menuBitmap
            };
            if (!InsertMenuItem(hMenu, menuItemCount++, true, ref menuItemInfo))
                return (HResult)Marshal.GetHRForLastWin32Error();
        }

        var selectedSrc = "";
        if (_srcArray.Count == 1)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(_srcArray[0]);
                if (attributes.HasFlag(FileAttributes.Directory))
                    selectedSrc = _srcArray[0];
            }
            catch
            {
                // ignore
            }
        }

        if (!string.IsNullOrEmpty(selectedSrc) || (_srcArray.Count == 0 && _dstArray.Count == 1))
        {
            MENUITEMINFO menuItemInfo = new()
            {
                fMask = MIIM.MIIM_ID | MIIM.MIIM_STRING | MIIM.MIIM_FTYPE | MIIM.MIIM_BITMAP,
                wID = idCmdFirst,
                fType = MFT.MFT_STRING,
                dwTypeData = "Paste",
                cch = 11,
                cbSize = (uint)Marshal.SizeOf<MENUITEMINFO>(),
                hbmpItem = _menuBitmap
            };
            if (!InsertMenuItem(hMenu, menuItemCount++, true, ref menuItemInfo))
                return (HResult)Marshal.GetHRForLastWin32Error();
        }

        sep = new MENUITEMINFO();
        sep.cbSize = (uint)Marshal.SizeOf(sep);
        sep.fMask = MIIM.MIIM_TYPE;
        sep.fType = MFT.MFT_SEPARATOR;
        if (!InsertMenuItem(hMenu, menuItemCount++, true, ref sep))
            return (HResult)Marshal.GetHRForLastWin32Error();

        return WinError.MAKE_HRESULT(WinError.SEVERITY_SUCCESS, 0, menuItemCount);
    }

    public void InvokeCommand(IntPtr pici)
    {
    }

    public void GetCommandString(UIntPtr idCmd, uint uFlags, IntPtr pReserved, StringBuilder pszName, uint cchMax)
    {
    }

    /// <summary>
    /// Initializes the context menu extension with the specified folder, data object, and registry key.
    /// </summary>
    /// <param name="pidlFolder"></param>
    /// <param name="pDataObj"></param>
    /// <param name="hKeyProgId"></param>
    public void Initialize(IntPtr pidlFolder, IntPtr pDataObj, IntPtr hKeyProgId)
    {
        if (pDataObj != IntPtr.Zero)
        {
            var fe = new FORMATETC
            {
                cfFormat = (short)CLIPFORMAT.CF_HDROP,
                ptd = IntPtr.Zero,
                dwAspect = DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                tymed = TYMED.TYMED_HGLOBAL
            };
            STGMEDIUM stm;
            var dataObject = (IDataObject)Marshal.GetObjectForIUnknown(pDataObj);
            dataObject.GetData(ref fe, out stm);

            try
            {
                var hDrop = stm.unionmember;
                if (hDrop == IntPtr.Zero)
                    throw new ArgumentException();

                var nFiles = DragQueryFile(hDrop, uint.MaxValue, null, 0);
                for (uint i = 0; i < nFiles; i++)
                {
                    StringBuilder pathBuilder = new(260);
                    if (DragQueryFile(hDrop, i, pathBuilder, pathBuilder.Capacity) == 0)
                        continue;
                    _srcArray.Add(pathBuilder.ToString());
                }


                if (_srcArray.Count == 0 && _dstArray.Count == 0)
                    Marshal.ThrowExceptionForHR(WinError.E_FAIL);
            }
            finally
            {
                ReleaseStgMedium(ref stm);
            }
        }

        StringBuilder folderPathBuilder = new(260);
        var res = SHGetPathFromIDListW(pidlFolder, folderPathBuilder);
        if (pidlFolder != IntPtr.Zero && res)
            _dstArray.Add(folderPathBuilder.ToString());
    }

    ~ContextMenuExt()
    {
        if (_menuBitmap != IntPtr.Zero)
        {
            DeleteObject(_menuBitmap);
            _menuBitmap = IntPtr.Zero;
        }
    }

    [ComRegisterFunction]
    public static void Register(Type t)
    {
    }

    [ComUnregisterFunction]
    public static void Unregister(Type t)
    {
    }
}