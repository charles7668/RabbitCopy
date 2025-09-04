using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Registry;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.UI.WindowsAndMessaging;
using static RabbitCopyContextMenu.ShellExt;
using FORMATETC = Windows.Win32.System.Com.FORMATETC;
using IDataObject = Windows.Win32.System.Com.IDataObject;

namespace RabbitCopyContextMenu;

[ClassInterface(ClassInterfaceType.None)]
[Guid("FE6B2057-2C0F-4461-9455-67116990409E")]
[ComVisible(true)]
public class ContextMenuExt : IShellExtInit, IContextMenu
{
    public ContextMenuExt()
    {
        using var ms = new MemoryStream(Resource.rabbit_white_16x16);
        try
        {
            var bitmap = new Bitmap(ms);
            _menuBitmap = bitmap.GetHbitmap();
        }
        catch
        {
            _menuBitmap = IntPtr.Zero;
        }

        _assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        var configIdentitiesFile = Path.Combine(_assemblyDir, "config-list.json");
        if (!File.Exists(configIdentitiesFile))
            return;
        try
        {
            using var sr = new StreamReader(configIdentitiesFile, Encoding.UTF8);
            var jsonContent = sr.ReadToEnd();
            var configList = JsonSerializer.Deserialize<List<ConfigIdentity>>(jsonContent);
            _configIdentities = configList ?? [];
        }
        catch
        {
            // ignore
        }
    }

    private const int COPY_MENU_ITEM_ID = 1;
    private const int OPEN_UI_MENU_ITEM_ID = 2;
    private const int PASTE_MENU_ITEM_ID = 3;
    private const int CONFIG_ID_OFFSET = PASTE_MENU_ITEM_ID + 1;
    private static List<string> _copyCandidate = [];
    private readonly List<ConfigIdentity> _configIdentities = [];
    private readonly List<string> _dstArray = [];
    private readonly List<string> _srcArray = [];

    private string _assemblyDir;
    private IntPtr _menuBitmap;

    public HRESULT QueryContextMenu(HMENU hMenu, uint indexMenu, uint idCmdFirst, uint idCmdLast, uint uFlags)
    {
        var cmfFlag = (CMF)uFlags;
        if (_srcArray.Count == 0 && _dstArray.Count == 0 && cmfFlag.HasFlag(CMF.CMF_NORMAL))
            return WinError.MAKE_HRESULT(WinError.SEVERITY_SUCCESS, 0, 0);

        if (cmfFlag.HasFlag(CMF.CMF_VERBSONLY | CMF.CMF_DEFAULTONLY))
            return WinError.MAKE_HRESULT(WinError.SEVERITY_SUCCESS, 0, 0);

        uint menuItemCount = 0;
        var sep = new MENUITEMINFOW();
        sep.cbSize = (uint)Marshal.SizeOf(sep);
        sep.fMask = MENU_ITEM_MASK.MIIM_TYPE;
        sep.fType = MENU_ITEM_TYPE.MFT_SEPARATOR;
        var menuHandle = new GlobalFreeSafeHandle(hMenu);
        if (!PInvoke.InsertMenuItem(menuHandle, menuItemCount++, true, sep))
            return (HRESULT)Marshal.GetHRForLastWin32Error();
        var subMenu = PInvoke.CreatePopupMenu();
        var menuBitmap = new HBITMAP(_menuBitmap);
        RegisterMenuItem(0, idCmdFirst, "RabbitCopy", true, menuBitmap, subMenu, menuItemCount++, hMenu);

        uint subMenuPos = 0;
        var enableCopy = _srcArray.Count > 0;
        RegisterMenuItem(COPY_MENU_ITEM_ID, idCmdFirst, "Copy", enableCopy, menuBitmap, HMENU.Null, subMenuPos++,
            subMenu);
        RegisterMenuItem(OPEN_UI_MENU_ITEM_ID, idCmdFirst, "GUI", enableCopy, menuBitmap, HMENU.Null, subMenuPos++,
            subMenu);

        var selectedSrc = "";
        if (_srcArray.Count == 1)
        {
            try
            {
                var attributes = File.GetAttributes(_srcArray[0]);
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
            RegisterMenuItem(PASTE_MENU_ITEM_ID, idCmdFirst, "Paste", true, menuBitmap, HMENU.Null, subMenuPos++,
                subMenu);
            for (var i = 0; i < _configIdentities.Count; i++)
                RegisterMenuItem((uint)(CONFIG_ID_OFFSET + i), idCmdFirst,
                    "Paste with conf - " + _configIdentities[i].Name, true, HBITMAP.Null,
                    HMENU.Null,
                    subMenuPos++, subMenu);
        }

        sep = new MENUITEMINFOW();
        sep.cbSize = (uint)Marshal.SizeOf(sep);
        sep.fMask = MENU_ITEM_MASK.MIIM_TYPE;
        sep.fType = MENU_ITEM_TYPE.MFT_SEPARATOR;
        if (!PInvoke.InsertMenuItem(menuHandle, menuItemCount, true, sep))
            return (HRESULT)Marshal.GetHRForLastWin32Error();

        return WinError.MAKE_HRESULT(WinError.SEVERITY_SUCCESS, 0, subMenuPos + 1);
    }

    public unsafe HRESULT InvokeCommand(CMINVOKECOMMANDINFO* pici)
    {
        var ici = pici;
        if (ici == null)
            return HRESULT.S_OK;
        var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if ((IntPtr)ici->lpVerb.Value == COPY_MENU_ITEM_ID)
        {
            _copyCandidate.Clear();
            _copyCandidate.AddRange(_srcArray);
        }
        else if ((IntPtr)ici->lpVerb.Value == PASTE_MENU_ITEM_ID)
            StartPaste(null);
        else if ((IntPtr)ici->lpVerb.Value == OPEN_UI_MENU_ITEM_ID)
        {
            if (_srcArray.Count == 0 || location == null)
                return HRESULT.S_OK;
            var exePath = Path.Combine(location, "RabbitCopy.exe");
            var src = _srcArray.Select(s =>
            {
                try
                {
                    var attributes = File.GetAttributes(s);
                    if (attributes.HasFlag(FileAttributes.Directory))
                        return s + "\\";
                    return s;
                }
                catch
                {
                    return "";
                }
            }).Where(s => s != "");
            var temp = src.ToList().ConvertAll(input => $"--files \"{input.Replace('\\', '/')}\"");
            string[] args = ["--open", string.Join(" ", temp)];
            ProcessStartInfo startInfo = new()
            {
                FileName = exePath,
                Arguments = string.Join(" ", args),
                UseShellExecute = true,
                CreateNoWindow = true
            };
            Process.Start(startInfo);
        }
        else if ((IntPtr)ici->lpVerb.Value >= CONFIG_ID_OFFSET)
        {
            var itemIndex = (IntPtr)ici->lpVerb.Value - CONFIG_ID_OFFSET;
            StartPaste(_configIdentities[(int)itemIndex].Guid);
        }

        void StartPaste(string? guid)
        {
            if (_copyCandidate.Count == 0 || _dstArray.Count == 0 || location == null)
                return;
            var exePath = Path.Combine(location, "RabbitCopy.exe");
            var src = _copyCandidate.Select(s =>
            {
                try
                {
                    var attributes = File.GetAttributes(s);
                    if (attributes.HasFlag(FileAttributes.Directory))
                        return s + "\\";
                    return s;
                }
                catch
                {
                    return "";
                }
            }).Where(s => s != "");
            var temp = src.ToList().ConvertAll(input => $"--files \"{input.Replace('\\', '/')}\"");
            if (guid != null)
                temp.Add($"--guid {guid}");

            string[] args = ["--dest", "\"" + _dstArray[0].Replace('\\', '/') + "\"", string.Join(" ", temp)];
            ProcessStartInfo startInfo = new()
            {
                FileName = exePath,
                Arguments = string.Join(" ", args),
                UseShellExecute = true,
                CreateNoWindow = true,
                WorkingDirectory = _assemblyDir
            };
            Process.Start(startInfo);
        }

        return HRESULT.S_OK;
    }

    public unsafe HRESULT GetCommandString(UIntPtr idCmd, uint uType, uint* pReserved, PSTR pszName, uint cchMax)
    {
        return HRESULT.S_OK;
    }

    public unsafe HRESULT Initialize(ITEMIDLIST* pidlFolder, IDataObject? pDataObj,
        HKEY hkeyProgID)
    {
        var bufferArray = new char[PInvoke.MAX_PATH];
        if (pDataObj != null)
        {
            var fe = new FORMATETC
            {
                cfFormat = (ushort)CLIPFORMAT.CF_HDROP,
                ptd = null,
                dwAspect = (uint)DVASPECT.DVASPECT_CONTENT,
                lindex = -1,
                tymed = (uint)TYMED.TYMED_HGLOBAL
            };
            pDataObj.GetData(fe, out var stm);

            try
            {
                var hDrop = (HDROP)PInvoke.GlobalLock(stm.u.hGlobal);

                var nFiles = PInvoke.DragQueryFile(hDrop, uint.MaxValue, null, 0);
                for (uint i = 0; i < nFiles; i++)
                {
                    uint textLen;
                    fixed (char* buffetPt = bufferArray)
                    {
                        PWSTR buffer = new(buffetPt);
                        textLen = PInvoke.DragQueryFile(hDrop, i, buffer, PInvoke.MAX_PATH);
                    }

                    if (textLen == 0)
                        continue;

                    _srcArray.Add(new string(bufferArray, 0, (int)textLen));
                }


                if (_srcArray.Count == 0 && _dstArray.Count == 0)
                    Marshal.ThrowExceptionForHR(WinError.E_FAIL);
            }
            finally
            {
                PInvoke.ReleaseStgMedium(ref stm);
            }
        }

        fixed (char* bufferPtr = bufferArray)
        {
            var buffer = new PWSTR(bufferPtr);
            var res = PInvoke.SHGetPathFromIDList(pidlFolder, buffer);
            if (pidlFolder != null && res == true)
                _dstArray.Add(buffer.ToString());
            else if (_srcArray.Count == 1)
            {
                var attributes = File.GetAttributes(_srcArray[0]);
                if (attributes.HasFlag(FileAttributes.Directory))
                    _dstArray.Add(_srcArray[0]);
            }
        }

        return HRESULT.S_OK;
    }

    ~ContextMenuExt()
    {
        if (_menuBitmap != IntPtr.Zero)
        {
            PInvoke.DeleteObject(new HGDIOBJ(_menuBitmap));
            _menuBitmap = IntPtr.Zero;
        }
    }

    public void InvokeCommand(IntPtr pici)
    {
    }

    [ComRegisterFunction]
    public static void Register(Type t)
    {
    }

    private unsafe int RegisterMenuItem(uint id,
        uint idCmdFirst,
        string text,
        bool enabled,
        HBITMAP bitmap,
        HMENU subMenu,
        uint position,
        HMENU registerTo)
    {
        var sub = new MENUITEMINFOW();
        sub.cbSize = (uint)Marshal.SizeOf(sub);

        var m = MENU_ITEM_MASK.MIIM_STRING | MENU_ITEM_MASK.MIIM_FTYPE | MENU_ITEM_MASK.MIIM_ID |
                MENU_ITEM_MASK.MIIM_STATE;
        if (bitmap != IntPtr.Zero)
            m |= MENU_ITEM_MASK.MIIM_BITMAP;
        if (subMenu != IntPtr.Zero)
            m |= MENU_ITEM_MASK.MIIM_SUBMENU;
        sub.fMask = m;

        sub.wID = idCmdFirst + id;
        sub.fType = MENU_ITEM_TYPE.MFT_STRING;
        fixed (char* textPtr = text)
        {
            sub.dwTypeData = textPtr;
        }

        sub.hSubMenu = subMenu;
        sub.fState = enabled ? MENU_ITEM_STATE.MFS_ENABLED : MENU_ITEM_STATE.MFS_DISABLED;
        sub.hbmpItem = bitmap;

        var registerToHandle = new GlobalFreeSafeHandle(registerTo);
        if (!PInvoke.InsertMenuItem(registerToHandle, position, true, sub))
            return Marshal.GetHRForLastWin32Error();

        return 0;
    }

    [ComUnregisterFunction]
    public static void Unregister(Type t)
    {
    }
}