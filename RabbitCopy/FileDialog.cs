using Microsoft.WindowsAPICodePack.Shell;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MS.WindowsAPICodePack.Internal;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using System.Text;
using static RabbitCopy.FileDialog;

namespace RabbitCopy;

#pragma warning disable

public static class FileDialog
{
    [Flags]
    public enum SHCONTF
    {
        CHECKING_FOR_CHILDREN = 0x00010,
        FOLDERS = 0x00020,
        NONFOLDERS = 0x00040,
        INCLUDEHIDDEN = 0x00080,
        INIT_ON_FIRST_NEXT = 0x00100,
        NETPRINTERSRCH = 0x00200,
        SHAREABLE = 0x00400,
        STORAGE = 0x00800,
        NAVIGATION_ENUM = 0x01000,
        FASTITEMS = 0x02000,
        SFLATLIST = 0x04000,
        ENABLE_ASYNC = 0x08000,
        INCLUDESUPERHIDDEN = 0x10000
    }

    [Flags]
    public enum SFGAO : uint
    {
        CANCOPY = 0x00000001,
        CANMOVE = 0x00000002,
        CANLINK = 0x00000004,
        STORAGE = 0x00000008,
        CANRENAME = 0x00000010,
        CANDELETE = 0x00000020,
        HASPROPSHEET = 0x00000040,
        DROPTARGET = 0x00000100,
        CAPABILITYMASK = 0x00000177,
        ENCRYPTED = 0x00002000,
        ISSLOW = 0x00004000,
        GHOSTED = 0x00008000,
        LINK = 0x00010000,
        SHARE = 0x00020000,
        READONLY = 0x00040000,
        HIDDEN = 0x00080000,
        DISPLAYATTRMASK = 0x000FC000,
        STREAM = 0x00400000,
        STORAGEANCESTOR = 0x00800000,
        VALIDATE = 0x01000000,
        REMOVABLE = 0x02000000,
        COMPRESSED = 0x04000000,
        BROWSABLE = 0x08000000,
        FILESYSANCESTOR = 0x10000000,
        FOLDER = 0x20000000,
        FILESYSTEM = 0x40000000,
        HASSUBFOLDER = 0x80000000,
        CONTENTSMASK = 0x80000000,
        STORAGECAPMASK = 0x70C50008,
    }

    public enum SIGDN : uint
    {
        NORMALDISPLAY = 0,
        PARENTRELATIVEPARSING = 0x80018001,
        PARENTRELATIVEFORADDRESSBAR = 0x8001c001,
        DESKTOPABSOLUTEPARSING = 0x80028000,
        PARENTRELATIVEEDITING = 0x80031001,
        DESKTOPABSOLUTEEDITING = 0x8004c000,
        FILESYSPATH = 0x80058000,
        URL = 0x80068000
    }

    public enum SICHINT : uint
    {
        DISPLAY = 0x00000000,
        CANONICAL = 0x10000000,
        ALLFIELDS = 0x80000000
    }

    public enum FDE_SHAREVIOLATION_RESPONSE
    {
        FDESVR_DEFAULT = 0,
        FDESVR_ACCEPT = 0x1,
        FDESVR_REFUSE = 0x2
    }

    public enum FDE_OVERWRITE_RESPONSE
    {
        FDEOR_DEFAULT = 0,
        FDEOR_ACCEPT = 0x1,
        FDEOR_REFUSE = 0x2
    }

    public enum FDAP
    {
        FDAP_BOTTOM = 0,
        FDAP_TOP = 0x1
    }

    [Flags]
    public enum FILEOPENDIALOGOPTIONS : uint
    {
        FOS_OVERWRITEPROMPT = 0x2,
        FOS_STRICTFILETYPES = 0x4,
        FOS_NOCHANGEDIR = 0x8,
        FOS_PICKFOLDERS = 0x20,
        FOS_FORCEFILESYSTEM = 0x40,
        FOS_ALLNONSTORAGEITEMS = 0x80,
        FOS_NOVALIDATE = 0x100,
        FOS_ALLOWMULTISELECT = 0x200,
        FOS_PATHMUSTEXIST = 0x800,
        FOS_FILEMUSTEXIST = 0x1000,
        FOS_CREATEPROMPT = 0x2000,
        FOS_SHAREAWARE = 0x4000,
        FOS_NOREADONLYRETURN = 0x8000,
        FOS_NOTESTFILECREATE = 0x10000,
        FOS_HIDEMRUPLACES = 0x20000,
        FOS_HIDEPINNEDPLACES = 0x40000,
        FOS_NODEREFERENCELINKS = 0x100000,
        FOS_OKBUTTONNEEDSINTERACTION = 0x200000,
        FOS_DONTADDTORECENT = 0x2000000,
        FOS_FORCESHOWHIDDEN = 0x10000000,
        FOS_DEFAULTNOMINIMODE = 0x20000000,
        FOS_FORCEPREVIEWPANEON = 0x40000000,
        FOS_SUPPORTSTREAMABLEITEMS = 0x80000000
    };

    [Flags]
    public enum CDCONTROLSTATEF : uint
    {
        CDCS_INACTIVE = 0x00000000,
        CDCS_ENABLED = 0x00000001,
        CDCS_VISIBLE = 0x00000002,
        CDCS_ENABLEDVISIBLE = 0x00000003
    }

    public enum SVGIO : uint
    {
        SVGIO_BACKGROUND = 0,
        SVGIO_SELECTION = 0x1,
        SVGIO_ALLVIEW = 0x2,
        SVGIO_CHECKED = 0x3,
        SVGIO_TYPE_MASK = 0xf,
        SVGIO_FLAG_VIEWORDER = 0x80000000
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct COMDLG_FILTERSPEC
    {
        public string pszName;
        public string pszSpec;
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    public interface IShellItem
    {
        HResult BindToHandler(IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid bhid,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr obj);

        [PreserveSig]
        HResult GetParent(out IShellItem ppsi);

        HResult GetDisplayName(SIGDN sigdnName, out StringBuilder displayName);

        [PreserveSig]
        HResult GetAttributes(SFGAO sfgaoMask, out SFGAO psfgaoAttribs);

        int Compare(IShellItem psi, SICHINT hint);
    };

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("b63ea76d-1f85-456f-a19c-48159efa858b")]
    public interface IShellItemArray
    {
        void BindToHandler(
            [In] IntPtr pbc,
            [In, MarshalAs(UnmanagedType.LPStruct)]
            Guid bhid,
            [In, MarshalAs(UnmanagedType.LPStruct)]
            Guid riid,
            [Out] out IntPtr ppv);

        void GetPropertyStore(
            [In] int flags,
            [In, MarshalAs(UnmanagedType.LPStruct)]
            Guid riid,
            [Out] out IntPtr ppv);

        void GetPropertyDescriptionList(
            [In] int keyType,
            [In, MarshalAs(UnmanagedType.LPStruct)]
            Guid riid,
            [Out] out IntPtr ppv);

        void GetAttributes(
            [In] int dwAttribFlags,
            [In] int sfgaoMask,
            [Out] out int psfgaoAttribs);

        void GetCount(
            [Out] out uint pdwNumItems);

        void GetItemAt(
            [In] uint dwIndex,
            [Out, MarshalAs(UnmanagedType.Interface)]
            out IShellItem ppsi);

        void EnumItems(
            [Out] out IntPtr ppenumShellItems);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("973510db-7d7f-452b-8975-74a85828d354")]
    public interface IFileDialogEvents
    {
        [PreserveSig]
        HResult OnFileOk(IFileDialog pfd);

        [PreserveSig]
        HResult OnFolderChanging(IFileDialog pfd, IShellItem psiFolder);

        [PreserveSig]
        HResult OnFolderChange(IFileDialog pfd);

        [PreserveSig]
        HResult OnSelectionChange(IFileDialog pfd);

        [PreserveSig]
        HResult OnShareViolation(IFileDialog pfd, IShellItem psi,
            out FDE_SHAREVIOLATION_RESPONSE pResponse);

        [PreserveSig]
        HResult OnTypeChange(IFileDialog pfd);

        [PreserveSig]
        HResult OnOverwrite(IFileDialog pfd, IShellItem psi, out FDE_OVERWRITE_RESPONSE pResponse);
    };

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("36116642-D713-4b97-9B83-7484A9D00433")]
    public interface IFileDialogControlEvents
    {
        HResult OnItemSelected(
            IFileDialogCustomize pfdc,
            uint dwIDCtl,
            uint dwIDItem);

        HResult OnButtonClicked(
            IFileDialogCustomize pfdc,
            uint dwIDCtl);

        HResult OnCheckButtonToggled(
            IFileDialogCustomize pfdc,
            uint dwIDCtl,
            [MarshalAs(UnmanagedType.Bool)] bool bChecked);

        HResult OnControlActivating(
            IFileDialogCustomize pfdc,
            uint dwIDCtl);
    }


    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
    public interface IFileDialog
    {
        [PreserveSig]
        HResult Show(IntPtr hwndParent);

        void SetFileTypes(uint cFileTypes,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
            COMDLG_FILTERSPEC[] rgFilterSpec);

        void SetFileTypeIndex(uint iFileType);

        uint GetFileTypeIndex();

        HResult Advise(IFileDialogEvents pfde, out ushort adviseCookie);

        void Unadvise(ushort dwCookie);

        void SetOptions(FILEOPENDIALOGOPTIONS fos);

        ushort GetOptions(out FILEOPENDIALOGOPTIONS pfos);

        void SetDefaultFolder(IShellItem psi);

        void SetFolder(IShellItem psi);

        HResult GetFolder(out IShellItem folder);

        HResult GetCurrentSelection(out IShellItem item);

        void SetFileName(string pszName);

        HResult GetFileName(out StringBuilder fileName);

        void SetTitle(string pszTitle);

        void SetOkButtonLabel(string pszText);

        void SetFileNameLabel(string pszLabel);

        HResult GetResult(out IShellItem shellItem);

        void AddPlace(IShellItem psi, FDAP fdap);

        void SetDefaultExtension(string pszDefaultExtension);

        void Close(HResult hr);

        void SetClientGuid(Guid guid);

        void ClearClientData();

        void SetFilter(IShellItemFilter pFilter);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("2659B475-EEB8-48b7-8F07-B378810F48CF")]
    public interface IShellItemFilter
    {
        int IncludeItem(IShellItem psi);
        int GetEnumFlagsForItem(IShellItem psi, out SHCONTF pgrfFlags);
    };

    [SuppressUnmanagedCodeSecurity]
    [ComImport, Guid("e6fdd21a-163f-4975-9c8c-a69f1ba37034"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFileDialogCustomize : IFileDialog
    {
        void EnableOpenDropDown(uint dwIDCtl);

        void AddMenu(uint dwIDCtl, [MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        void AddPushButton(uint dwIDCtl, [MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        void AddComboBox(uint dwIDCtl);

        void AddRadioButtonList(uint dwIDCtl);

        void AddCheckButton(uint dwIDCtl, [MarshalAs(UnmanagedType.LPWStr)] string pszLabel,
            [MarshalAs(UnmanagedType.Bool)] bool bChecked);

        void AddEditBox(uint dwIDCtl, [MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void AddSeparator(uint dwIDCtl);
        void AddText(uint dwIDCtl, [MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetControlLabel(uint dwIDCtl, [MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        CDCONTROLSTATEF GetControlState(uint dwIDCtl);
        void SetControlState(uint dwIDCtl, CDCONTROLSTATEF dwState);

        void GetEditBoxText(uint dwIDCtl,
            out StringBuilder ppszText);

        void SetEditBoxText(uint dwIDCtl, [MarshalAs(UnmanagedType.LPWStr)] string pszText);

        [return: MarshalAs(UnmanagedType.Bool)]
        bool GetCheckButtonState(uint dwIDCtl);

        void SetCheckButtonState(uint dwIDCtl, [MarshalAs(UnmanagedType.Bool)] bool bChecked);

        void AddControlItem(uint dwIDCtl, int dwIDItem, [MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        void RemoveControlItem(uint dwIDCtl, int dwIDItem);

        void RemoveAllControlItems(uint dwIDCtl);

        CDCONTROLSTATEF GetControlItemState(uint dwIDCtl, int dwIDItem);

        void SetControlItemState(uint dwIDCtl, int dwIDItem, CDCONTROLSTATEF dwState);
        uint GetSelectedControlItem(uint dwIDCtl);
        void SetSelectedControlItem(uint dwIDCtl, int dwIDItem);
        void StartVisualGroup(uint dwIDCtl, [MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

        void EndVisualGroup();

        void MakeProminent(uint dwIDCtl);
    }

    [ComImport]
    [Guid("FC4801A3-2BA9-11CF-A229-00AA003D7352")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IObjectWithSite
    {
        void SetSite([In, MarshalAs(UnmanagedType.IUnknown)] object pUnkSite);
        void GetSite(ref Guid riid, out IntPtr ppvSite);
    }

    [DllImport("shlwapi.dll")]
    internal static extern int IUnknown_QueryService(IntPtr pUnk, ref Guid guidService, ref Guid riid,
        [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    [ComImport]
    [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IServiceProvider
    {
        HResult QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppv);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("cde725b0-ccc9-4519-917e-325d72fab4ce")]
    public interface IFolderView
    {
        HResult GetCurrentViewMode(out uint pViewMode);

        HResult SetCurrentViewMode(uint ViewMode);

        HResult GetFolder(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

        HResult Item(int iItemIndex, out IntPtr ppidl);

        HResult ItemCount(uint uFlags, out int pcItems);

        HResult Items(SVGIO uFlags, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

        HResult GetSelectionMarkedItem(out int piItem);

        HResult GetFocusedItem(out int piItem);

        HResult GetItemPosition(IntPtr pidl, out POINT ppt);

        HResult GetSpacing(out POINT ppt);

        HResult GetDefaultSpacing(out POINT ppt);

        [return: MarshalAs(UnmanagedType.Bool)]
        bool GetAutoArrange();

        HResult SelectItem(int iItem, uint dwFlags);

        HResult SelectAndPositionItems(
            uint cidl,
            IntPtr apidl,
            IntPtr apt,
            uint dwFlags);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROPVARIANT
    {
        public ushort vt;
        public ushort wReserved1;
        public ushort wReserved2;
        public ushort wReserved3;
        public IntPtr data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROPERTYKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SORTCOLUMN
    {
        public PROPERTYKEY propkey;
        public int direction; // SortDirection enumeration
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("1af3a467-214f-4298-908e-06b03e0b39f9")]
    public interface IFolderView2 : IFolderView
    {
        HResult SetGroupBy([In] ref PROPERTYKEY key, [In, MarshalAs(UnmanagedType.Bool)] bool fAscending);

        HResult GetGroupBy(out PROPERTYKEY pkey, [Out, MarshalAs(UnmanagedType.Bool)] out bool pfAscending);

        HResult SetViewProperty([In] IntPtr pidl, [In] ref PROPERTYKEY propkey, [In] ref PROPVARIANT propvar);

        HResult GetViewProperty([In] IntPtr pidl, [In] ref PROPERTYKEY propkey, [Out] out PROPVARIANT ppropvar);

        HResult SetTileViewProperties([In] IntPtr pidl, [In, MarshalAs(UnmanagedType.LPWStr)] string pszPropList);

        HResult SetExtendedTileViewProperties([In] IntPtr pidl,
            [In, MarshalAs(UnmanagedType.LPWStr)] string pszPropList);

        HResult SetText([In] uint iType, [In, MarshalAs(UnmanagedType.LPWStr)] string pwszText);

        HResult SetCurrentFolderFlags([In] uint dwMask, [In] uint dwFlags);

        HResult GetCurrentFolderFlags(out uint pdwFlags);

        HResult GetSortColumnCount(out int pcColumns);

        HResult SetSortColumns([In] SORTCOLUMN[] rgSortColumns, [In] int cColumns);

        HResult GetSortColumns([Out] SORTCOLUMN[] rgSortColumns, [In] int cColumns);

        HResult GetItem([In] int iItem, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.IUnknown)] out object ppv);

        HResult GetVisibleItem([In] int iStart, [In, MarshalAs(UnmanagedType.Bool)] bool fPrevious, out int piItem);

        HResult GetSelectedItem([In] int iStart, out int piItem);

        [PreserveSig]
        HResult GetSelection([In, MarshalAs(UnmanagedType.Bool)] bool fNoneImpliesFolder,
            [Out] out IShellItemArray? ppsia);

        HResult GetSelectionState([In] IntPtr pidl, out uint pdwFlags);

        HResult InvokeVerbOnSelection([In, MarshalAs(UnmanagedType.LPStr)] string pszVerb);

        HResult SetViewModeAndIconSize([In] uint uViewMode, [In] int iImageSize);

        HResult GetViewModeAndIconSize(out uint puViewMode, out int piImageSize);

        HResult SetGroupSubsetCount([In] uint cVisibleRows);

        HResult GetGroupSubsetCount(out uint pcVisibleRows);

        HResult SetRedraw([In, MarshalAs(UnmanagedType.Bool)] bool fRedrawOn);

        HResult IsMoveInSameFolder();

        HResult DoRename();
    }

    [ComImport]
    [Guid("000214E6-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellFolder
    {
        HResult BindToObject(
            [In] IntPtr pidl,
            [In] IBindCtx pbc,
            [In] ref Guid riid,
            [Out, MarshalAs(UnmanagedType.IUnknown)]
            out object ppv);
    }

    [DllImport("ole32.dll", PreserveSig = false)]
    public static extern void CoCreateInstance(
        [In] ref Guid rclsid,
        [In, MarshalAs(UnmanagedType.IUnknown)]
        object? pUnkOuter,
        [In] uint dwClsContext,
        [In] ref Guid riid,
        [Out, MarshalAs(UnmanagedType.Interface)]
        out object ppv);

    [ComImport,
     Guid("d57c7288-d4ad-4768-be02-9d969532d960"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IFileOpenDialog : IFileDialog
    {
        void GetResults([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppenum);
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetDlgItem(IntPtr hDlg, int nIDDlgItem);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);


    public const uint GW_CHILD = 5;
    public const uint GW_HWNDNEXT = 2;
    public const int MAX_PATH = 260;
    public const int SW_HIDE = 0;
    public const uint BM_SETSTYLE = 0x00F4;
    public const ulong BS_DEFPUSHBUTTON = 0x00000001L;

    public const uint WM_SIZE = 0x0005;
    public const uint WM_APP = 0x8000;

    public const int GWLP_WNDPROC = -4;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

    public delegate IntPtr WinProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WinProc newProc);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, WinProc newProc);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam,
        IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
    public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool PostMessageA(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MoveWindow(
        IntPtr hWnd,
        int X,
        int Y,
        int nWidth,
        int nHeight,
        bool bRepaint);

    [ComImport, Guid("00000114-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IOleWindow
    {
        [PreserveSig]
        HResult GetWindow(out IntPtr phwnd);

        [PreserveSig]
        HResult ContextSensitiveHelp([In] bool fEnterMode);
    }

    [ComImport]
    [Guid("0000000e-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IBindCtx
    {
        // HRESULT RegisterObjectBound(IUnknown* punk)
        void RegisterObjectBound([In, MarshalAs(UnmanagedType.IUnknown)] object punk);

        // HRESULT RevokeObjectBound(IUnknown* punk)
        void RevokeObjectBound([In, MarshalAs(UnmanagedType.IUnknown)] object punk);

        // HRESULT ReleaseBoundObjects()
        void ReleaseBoundObjects();

        // HRESULT SetBindOptions(BIND_OPTS* pbindopts)
        void SetBindOptions([In] ref BIND_OPTS pbindopts);

        // HRESULT GetBindOptions(BIND_OPTS* pbindopts)
        void GetBindOptions([In, Out] ref BIND_OPTS pbindopts);

        // HRESULT GetRunningObjectTable(IRunningObjectTable** pprot)
        void GetRunningObjectTable([Out] out IRunningObjectTable pprot);

        // HRESULT RegisterObjectParam(LPOLESTR pszKey, IUnknown* punk)
        void RegisterObjectParam([In, MarshalAs(UnmanagedType.LPWStr)] string pszKey,
            [In, MarshalAs(UnmanagedType.IUnknown)]
            object punk);

        // HRESULT GetObjectParam(LPOLESTR pszKey, IUnknown** ppunk)
        void GetObjectParam([In, MarshalAs(UnmanagedType.LPWStr)] string pszKey,
            [Out, MarshalAs(UnmanagedType.IUnknown)]
            out object ppunk);

        // HRESULT EnumObjectParam(IEnumString** ppenum)
        void EnumObjectParam([Out] out IEnumString ppenum);

        // HRESULT RevokeObjectParam(LPOLESTR pszKey)
        void RevokeObjectParam([In, MarshalAs(UnmanagedType.LPWStr)] string pszKey);
    }
}

public class FileOkEventArgs : EventArgs
{
    public IFileDialog FileDialog { get; }
    public HResult Result { get; set; } = HResult.Ok;

    public FileOkEventArgs(IFileDialog fileDialog)
    {
        FileDialog = fileDialog;
    }
}

public class SelectionChangeEventArgs : EventArgs
{
    public IFileDialog FileDialog { get; }
    public IShellItem? SelectedItem { get; private set; }
    public HResult Result { get; set; } = HResult.Ok;

    public SelectionChangeEventArgs(IFileDialog fileDialog)
    {
        FileDialog = fileDialog;
        try
        {
            fileDialog.GetCurrentSelection(out var item);
            SelectedItem = item;
        }
        catch
        {
            SelectedItem = null;
        }
    }

    public void SetSelectedItem(IShellItem item)
    {
        SelectedItem = item;
    }
}

public class FolderChangingEventArgs : EventArgs
{
    public IFileDialog FileDialog { get; }
    public IShellItem? SelectedFolder { get; private set; }
    public HResult Result { get; set; } = HResult.Ok;

    public FolderChangingEventArgs(IFileDialog fileDialog, IShellItem selectedFolder)
    {
        FileDialog = fileDialog;
        SelectedFolder = selectedFolder;
    }

    public void SetSelectedFolder(IShellItem folder)
    {
        SelectedFolder = folder;
    }
}

public class FileOpenDialog
{
    private IntPtr _hSel = IntPtr.Zero;
    private IntPtr _hOk = IntPtr.Zero;
    private IntPtr _hWnd = IntPtr.Zero;

    private const uint WM_POSITION = WM_APP + 100;

    private IntPtr NewWndProc(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam)
    {
        switch (Msg)
        {
            case WM_SIZE:
                if (_hSel == IntPtr.Zero)
                {
                    break;
                }

                PostMessage(_hWnd, WM_POSITION, IntPtr.Zero, IntPtr.Zero);
                break;
            case WM_POSITION:
                RECT orc = new RECT();
                RECT src = new RECT();
                GetWindowRect(_hOk, ref orc);
                GetWindowRect(_hSel, ref src);
                POINT orcPt = new POINT()
                {
                    X = orc.left,
                    Y = orc.top
                };
                POINT srcPt = new POINT()
                {
                    X = src.left,
                    Y = src.top
                };
                ScreenToClient(_hWnd, ref orcPt);
                ScreenToClient(_hWnd, ref srcPt);
                MoveWindow(_hSel, srcPt.X, srcPt.Y, orc.right - src.left,
                    orc.bottom - src.top, true);
                break;
            default:
                break;
        }

        return CallWindowProc(_originalWndProc, hWnd, Msg, wParam, lParam);
    }

    private class FileDialogEventsHandler(FileOpenDialog parentDialog) : IFileDialogEvents, IFileDialogControlEvents
    {
        public Func<IFileDialog, HResult>? OnFileOkEvent { get; set; }
        public Func<IFileDialog, HResult>? OnSelectionChangeEvent { get; set; }
        public Func<IFileDialog, IShellItem, HResult>? OnFolderChangingEvent { get; set; }

        private bool once = true;
        private const uint ID_SELECT = 601;

        public HResult OnFileOk(IFileDialog pfd)
        {
            if (OnFileOkEvent != null)
            {
                return OnFileOkEvent(pfd);
            }

            return HResult.Ok;
        }

        public HResult OnFolderChanging(IFileDialog pfd, IShellItem psiFolder)
        {
            if (once)
            {
                if (pfd is IOleWindow pWindow)
                {
                    var hr = pWindow.GetWindow(out IntPtr hwndDialog);

                    parentDialog._hWnd = hwndDialog;
                    if (hr == HResult.Ok)
                    {
                        const int IDOK = 1;
                        IntPtr hOk = GetDlgItem(hwndDialog, IDOK);
                        parentDialog._hOk = hOk;
                        for (IntPtr h = GetWindow(hwndDialog, GW_CHILD);
                             h != IntPtr.Zero;
                             h = GetWindow(h, GW_HWNDNEXT))
                        {
                            StringBuilder wbuf = new StringBuilder();

                            if (GetWindowTextW(h, wbuf, MAX_PATH) > 0 && wbuf.ToString() == "Select")
                            {
                                parentDialog._hSel = h;
                                ShowWindow(hOk, SW_HIDE);
                                EnableWindow(hOk, false);
                                SendMessage(parentDialog._hSel, BM_SETSTYLE, (IntPtr)BS_DEFPUSHBUTTON, (IntPtr)1);
                                parentDialog._newWndProcDelegate = parentDialog.NewWndProc;
                                parentDialog._originalWndProc =
                                    GetWindowLongPtr(parentDialog._hWnd, GWLP_WNDPROC);
                                SetWindowLongPtrInternal(parentDialog._hWnd, GWLP_WNDPROC,
                                    parentDialog._newWndProcDelegate);

                                break;
                            }
                        }
                    }
                }

                once = false;
            }

            if (OnFolderChangingEvent != null)
            {
                return OnFolderChangingEvent(pfd, psiFolder);
            }

            return HResult.Ok;
        }

        public HResult OnFolderChange(IFileDialog pfd)
        {
            return HResult.Ok;
        }

        public HResult OnSelectionChange(IFileDialog pfd)
        {
            if (OnSelectionChangeEvent != null)
            {
                return OnSelectionChangeEvent(pfd);
            }

            return HResult.Ok;
        }


        public HResult OnShareViolation(IFileDialog pfd, IShellItem psi, out FDE_SHAREVIOLATION_RESPONSE pResponse)
        {
            pResponse = FDE_SHAREVIOLATION_RESPONSE.FDESVR_DEFAULT;
            return HResult.Ok;
        }

        public HResult OnTypeChange(IFileDialog pfd)
        {
            return HResult.Ok;
        }

        public HResult OnOverwrite(IFileDialog pfd, IShellItem psi, out FDE_OVERWRITE_RESPONSE pResponse)
        {
            pResponse = FDE_OVERWRITE_RESPONSE.FDEOR_DEFAULT;
            return HResult.Ok;
        }

        public HResult OnItemSelected(IFileDialogCustomize pfdc, uint dwIDCtl, uint dwIDItem)
        {
            return HResult.Ok;
        }


        public HResult OnButtonClicked(IFileDialogCustomize customFd, uint dwIDCtl)
        {
            if (dwIDCtl == ID_SELECT)
            {
                IFileDialog fd = customFd;
                Guid IID_ServiceProvider = new Guid("6d5140c1-7436-11ce-8034-00aa006009fa");
                IntPtr pUnk = Marshal.GetIUnknownForObject(fd);
                Marshal.QueryInterface(pUnk, ref IID_ServiceProvider, out var psp);
                FileDialog.IServiceProvider sp = (FileDialog.IServiceProvider)Marshal.GetObjectForIUnknown(psp);
                Guid IID_IFolderView = new Guid("CDE725B0-CCC9-4519-917E-325D72FAB4CE");
                Guid IID_IFolderView2 = new Guid("1af3a467-214f-4298-908e-06b03e0b39f9");
                sp.QueryService(ref IID_IFolderView, ref IID_IFolderView2, out IntPtr ppv);
                IFolderView fv = (IFolderView)Marshal.GetUniqueObjectForIUnknown(ppv);
                IntPtr folderViewPtr = Marshal.GetIUnknownForObject(fv);
                var hr = Marshal.QueryInterface(folderViewPtr, ref IID_IFolderView2, out IntPtr folderView2Ptr);
                if (hr == 0 && folderView2Ptr != IntPtr.Zero)
                {
                    IFolderView2 fv2 = (IFolderView2)Marshal.GetObjectForIUnknown(folderView2Ptr);
                    fv2.GetSelectionMarkedItem(out var picnt);
                    fv2.ItemCount(0, out var cont);
                    Guid IID_IShellFolder = new Guid("000214E6-0000-0000-C000-000000000046");
                    Guid IID_IShellItem = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");
                    var IID_IShellItemArrayGuid = new Guid("b63ea76d-1f85-456f-a19c-48159efa858b");
                    fv2.Items(SVGIO.SVGIO_SELECTION, ref IID_IShellItemArrayGuid, out var shobj);
                    IShellItemArray shellItemArray = (IShellItemArray)shobj;
                    shellItemArray.GetCount(out var cnt);
                    for (int i = 0; i < cnt; i++)
                    {
                        shellItemArray.GetItemAt((uint)i, out var item);
                        item.GetDisplayName(SIGDN.FILESYSPATH, out StringBuilder path);
                        parentDialog.SelectedTargets.Add(path.ToString());
                    }
                }

                fd.Close(HResult.Ok);
            }

            return HResult.Ok;
        }

        public HResult OnCheckButtonToggled(IFileDialogCustomize pfdc, uint dwIDCtl, bool bChecked)
        {
            return HResult.Ok;
        }

        public HResult OnControlActivating(IFileDialogCustomize pfdc, uint dwIDCtl)
        {
            return HResult.Ok;
        }
    }

    public event EventHandler<FileOkEventArgs>? OnFileOk;
    public event EventHandler<SelectionChangeEventArgs>? OnSelectionChange;
    public event EventHandler<FolderChangingEventArgs>? OnFolderChanging;
    private WinProc _newWndProcDelegate;
    private IntPtr _originalWndProc;

    public List<string> SelectedTargets { get; } = new();

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8 ? GetWindowLongPtr64(hWnd, nIndex) : GetWindowLong(hWnd, nIndex);
    }

    private static IntPtr SetWindowLongPtrInternal(IntPtr hWnd, int nIndex, WinProc newProc)
    {
        return IntPtr.Size == 8 ? SetWindowLongPtr(hWnd, nIndex, newProc) : SetWindowLong(hWnd, nIndex, newProc);
    }

    public void ShowDialog()
    {
        SelectedTargets.Clear();

        Guid CLSID_FileOpenDialog = new Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7");
        Guid IID_IFileOpenDialog = new Guid("D57C7288-D4AD-4768-BE02-9D969532D960");
        object? comObject = null;
        CoCreateInstance(ref CLSID_FileOpenDialog, null, 1,
            ref IID_IFileOpenDialog,
            out comObject);


        FileDialogEventsHandler eventsHandler = new FileDialogEventsHandler(this)
        {
            OnFileOkEvent = dialog =>
            {
                var args = new FileOkEventArgs(dialog);
                OnFileOk?.Invoke(this, args);
                return args.Result;
            },
            OnSelectionChangeEvent = dialog =>
            {
                var args = new SelectionChangeEventArgs(dialog);
                OnSelectionChange?.Invoke(this, args);
                return args.Result;
            },
            OnFolderChangingEvent = (dialog, folder) =>
            {
                var args = new FolderChangingEventArgs(dialog, folder);
                OnFolderChanging?.Invoke(this, args);
                return args.Result;
            }
        };

        IFileDialogCustomize customize = (IFileDialogCustomize)comObject;
        customize.AddPushButton(601, "Select");
        IFileDialog pDialog = (IFileOpenDialog)comObject;
        FILEOPENDIALOGOPTIONS options;
        pDialog.GetOptions(out options);
        options = options |
                  FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM |
                  FILEOPENDIALOGOPTIONS.FOS_FILEMUSTEXIST |
                  FILEOPENDIALOGOPTIONS.FOS_ALLOWMULTISELECT;
        pDialog.SetOptions(options);
        pDialog.Advise(eventsHandler, out var adviseCookie);

        try
        {
            pDialog.Show(IntPtr.Zero);
        }
        finally
        {
            pDialog.Unadvise(adviseCookie);
        }
    }
}

#pragma warning restore