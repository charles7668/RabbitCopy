using System.Diagnostics;
using System.Drawing;
using System.Reflection;
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
    private const int COPY_MENU_ITEM_ID = 1;
    private const int PASTE_MENU_ITEM_ID = 2;
    private static List<string> _copyCandidate = [];

    private int RegisterMenuItem(uint id,
        uint idCmdFirst,
        string text,
        bool enabled,
        IntPtr bitmap,
        IntPtr subMenu,
        uint position,
        IntPtr registerTo)
    {
        var sub = new MENUITEMINFO();
        sub.cbSize = (uint)Marshal.SizeOf(sub);

        var m = MIIM.MIIM_STRING | MIIM.MIIM_FTYPE | MIIM.MIIM_ID | MIIM.MIIM_STATE;
        if (bitmap != IntPtr.Zero)
            m |= MIIM.MIIM_BITMAP;
        if (subMenu != IntPtr.Zero)
            m |= MIIM.MIIM_SUBMENU;
        sub.fMask = m;

        sub.wID = idCmdFirst + id;
        sub.fType = MFT.MFT_STRING;
        sub.dwTypeData = text;
        sub.hSubMenu = subMenu;
        sub.fState = enabled ? MFS.MFS_ENABLED : MFS.MFS_DISABLED;
        sub.hbmpItem = bitmap;

        if (!InsertMenuItem(registerTo, position, true, ref sub))
            return Marshal.GetHRForLastWin32Error();
        return 0;
    }

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
        var subMenu = CreatePopupMenu();
        RegisterMenuItem(0, idCmdFirst, "RabbitCopy", true, _menuBitmap, subMenu, menuItemCount++, hMenu);
        if (_srcArray.Count > 0)
        {
            RegisterMenuItem(COPY_MENU_ITEM_ID, idCmdFirst, "Copy", true, _menuBitmap, IntPtr.Zero, 0, subMenu);
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
            RegisterMenuItem(PASTE_MENU_ITEM_ID, idCmdFirst, "Paste", true, _menuBitmap, IntPtr.Zero, 1, subMenu);
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
        var ici = (CMINVOKECOMMANDINFO?)Marshal.PtrToStructure(pici, typeof(CMINVOKECOMMANDINFO));
        if (ici == null)
            return;
        var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (ici.Value.verb == COPY_MENU_ITEM_ID)
        {
            _copyCandidate.Clear();
            _copyCandidate.AddRange(_srcArray);
        }
        else if (ici.Value.verb == PASTE_MENU_ITEM_ID)
        {
            if (_copyCandidate.Count == 0 || _dstArray.Count == 0 || location == null)
                return;
            var exePath = Path.Combine(location, "RabbitCopy.exe");
            var src = _copyCandidate.Select(s =>
            {
                try
                {
                    FileAttributes attributes = File.GetAttributes(s);
                    if (attributes.HasFlag(FileAttributes.Directory))
                        return s + "\\";
                    return s;
                }
                catch
                {
                    return "";
                }
            }).Where(s => s != "");
            var temp = src.ToList().ConvertAll(input => $"--files {input}");
            string[] args = ["--dest", _dstArray[0], string.Join(" ", temp)];
            ProcessStartInfo startInfo = new()
            {
                FileName = exePath,
                Arguments = string.Join(" ", args),
                UseShellExecute = true,
                CreateNoWindow = true
            };
            Process.Start(startInfo);
        }
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