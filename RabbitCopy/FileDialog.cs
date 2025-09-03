using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.System.Ole;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using RabbitCopy.Helper;
using IServiceProvider = Windows.Win32.System.Com.IServiceProvider;

namespace RabbitCopy;

#pragma warning disable

public class FileOkEventArgs : EventArgs
{
    public FileOkEventArgs(IFileDialog fileDialog)
    {
        FileDialog = fileDialog;
    }

    public IFileDialog FileDialog { get; }

    public HRESULT Result { get; } = HRESULT.S_OK;
}

public class SelectionChangeEventArgs : EventArgs
{
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

    public IFileDialog FileDialog { get; }

    public IShellItem? SelectedItem { get; private set; }

    public HRESULT Result { get; } = HRESULT.S_OK;
}

public class FolderChangingEventArgs : EventArgs
{
    public FolderChangingEventArgs(IFileDialog fileDialog, IShellItem selectedFolder)
    {
        FileDialog = fileDialog;
        SelectedFolder = selectedFolder;
    }

    public IFileDialog FileDialog { get; }

    public IShellItem? SelectedFolder { get; private set; }

    public HRESULT Result { get; } = HRESULT.S_OK;
}

[Flags]
public enum FileDialogFlag
{
    PICK_FOLDER = 0b1,
    MULTI_SELECT = 0b10
}

public partial class FileOpenDialog(FileDialogFlag fileDialogFlag)
{
    private const uint WM_POSITION = PInvoke.WM_APP + 100;

    private string _enteredDir = string.Empty;
    private HWND _hOk = HWND.Null;
    private HWND _hSel = HWND.Null;
    private HWND _hWnd = HWND.Null;

    private WNDPROC _newWndProcDelegate;
    private IntPtr _originalWndProc;

    public List<string> SelectedTargets { get; } = new();

    private static IntPtr GetWindowLongPtrInternal(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex)
    {
        return IntPtr.Size == 8
            ? Win32Helper.User32.GetWindowLongPtr(hWnd, nIndex)
            : PInvoke.GetWindowLong(hWnd, nIndex);
    }

    private LRESULT NewWndProc(HWND hWnd, uint Msg, WPARAM wParam, LPARAM lParam)
    {
        switch (Msg)
        {
            case PInvoke.WM_SIZE:
                if (_hSel == IntPtr.Zero)
                    break;

                PInvoke.PostMessage(_hWnd, WM_POSITION, new WPARAM(), IntPtr.Zero);
                break;
            case WM_POSITION:
                PInvoke.GetWindowRect(_hOk, out var orc);
                PInvoke.GetWindowRect(_hSel, out var src);
                Point orcPt = new Point
                {
                    X = orc.left,
                    Y = orc.top
                };
                Point srcPt = new Point
                {
                    X = src.left,
                    Y = src.top
                };

                PInvoke.ScreenToClient(_hWnd, ref orcPt);
                PInvoke.ScreenToClient(_hWnd, ref srcPt);
                PInvoke.MoveWindow(_hSel, srcPt.X, srcPt.Y, orc.right - src.left,
                    orc.bottom - src.top, true);
                break;
        }

        var originalWndProc = Marshal.GetDelegateForFunctionPointer<WNDPROC>(_originalWndProc);

        return PInvoke.CallWindowProc(originalWndProc, hWnd, Msg, wParam, lParam);
    }

    public event EventHandler<FileOkEventArgs>? OnFileOk;

    public event EventHandler<FolderChangingEventArgs>? OnFolderChanging;

    public event EventHandler<SelectionChangeEventArgs>? OnSelectionChange;

    private static IntPtr SetWindowLongPtrInternal(HWND hWnd, WINDOW_LONG_PTR_INDEX nIndex, IntPtr newProc)
    {
        return IntPtr.Size == 8
            ? Win32Helper.User32.SetWindowLongPtr(hWnd, nIndex, newProc)
            : PInvoke.SetWindowLong(hWnd, nIndex, (int)newProc);
    }

    public bool ShowDialog()
    {
        SelectedTargets.Clear();

        Guid CLSID_FileOpenDialog = new Guid(COMHelper.CLSID.FILE_OPEN_DIALOG);
        Guid IID_IFileOpenDialog = new Guid(COMHelper.IID.FILE_OPEN_DIALOG);
        object? comObject;
        PInvoke.CoCreateInstance(ref CLSID_FileOpenDialog, null, CLSCTX.CLSCTX_INPROC_SERVER,
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
        pDialog.GetOptions(out var options);
        options = options |
                  FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM |
                  FILEOPENDIALOGOPTIONS.FOS_FILEMUSTEXIST |
                  (fileDialogFlag.HasFlag(FileDialogFlag.PICK_FOLDER) ? FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS : 0) |
                  (fileDialogFlag.HasFlag(FileDialogFlag.MULTI_SELECT)
                      ? FILEOPENDIALOGOPTIONS.FOS_ALLOWMULTISELECT
                      : 0);
        pDialog.SetOptions(options);
        pDialog.Advise(eventsHandler, out var adviseCookie);

        try
        {
            pDialog.Show(HWND.Null);
            if (SelectedTargets.Count != 0)
                return true;
            pDialog.GetResult(out var selectedShellItem);
            if (selectedShellItem != null)
            {
                selectedShellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var selectedFile);
                SelectedTargets.Add(selectedFile.ToString());
                return true;
            }

            SelectedTargets.Add(_enteredDir);
            return true;
        }
        catch (COMException ex)
        {
        }
        finally
        {
            pDialog.Unadvise(adviseCookie);
        }

        return false;
    }

    private class FileDialogEventsHandler(FileOpenDialog parentDialog) : IFileDialogEvents, IFileDialogControlEvents
    {
        private const uint ID_SELECT = 601;
        private const int ID_OK = 1;

        private readonly char[] _textBuffer = new char[PInvoke.MAX_PATH];

        private bool _once = true;

        public Func<IFileDialog, HRESULT>? OnFileOkEvent { get; init; }

        public Func<IFileDialog, HRESULT>? OnSelectionChangeEvent { get; init; }

        public Func<IFileDialog, IShellItem, HRESULT>? OnFolderChangingEvent { get; init; }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        void IFileDialogControlEvents.OnButtonClicked(IFileDialogCustomize customFd, uint dwIDCtl)
        {
            if (dwIDCtl != ID_SELECT) return;

            ProcessSelectedItems((IFileDialog)customFd);
        }

        public void OnCheckButtonToggled(IFileDialogCustomize pfdc, uint dwIDCtl, BOOL bChecked)
        {
        }

        void IFileDialogControlEvents.OnControlActivating(IFileDialogCustomize pfdc, uint dwIDCtl)
        {
        }

        void IFileDialogControlEvents.OnItemSelected(IFileDialogCustomize pfdc, uint dwIDCtl, uint dwIDItem)
        {
        }

        [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
        void IFileDialogEvents.OnFolderChanging(IFileDialog pfd, IShellItem psiFolder)
        {
            if (_once)
            {
                if (pfd is IOleWindow pWindow)
                {
                    try
                    {
                        pWindow.GetWindow(out HWND hwndDialog);
                        parentDialog._hWnd = hwndDialog;
                        HWND hOk = PInvoke.GetDlgItem(hwndDialog, ID_OK);
                        parentDialog._hOk = hOk;

                        for (HWND h = PInvoke.GetWindow(hwndDialog, GET_WINDOW_CMD.GW_CHILD);
                             h != HWND.Null;
                             h = PInvoke.GetWindow(h, GET_WINDOW_CMD.GW_HWNDNEXT))
                            if (IsSelectButton(h))
                            {
                                ConfigureSelectButton(h, hOk, hwndDialog);
                                break;
                            }
                    }
                    catch (COMException)
                    {
                    }
                }

                _once = false;
            }

            OnFolderChangingEvent?.Invoke(pfd, psiFolder);
        }

        void IFileDialogEvents.OnFolderChange(IFileDialog pfd)
        {
            try
            {
                pfd.GetFolder(out var folderShellItem);
                folderShellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var dirPathBuilder);
                parentDialog._enteredDir = dirPathBuilder.ToString();
            }
            catch (COMException)
            {
            }
        }

        void IFileDialogEvents.OnSelectionChange(IFileDialog pfd)
        {
            OnSelectionChangeEvent?.Invoke(pfd);
        }

        public unsafe void OnShareViolation(IFileDialog pfd, IShellItem psi, FDE_SHAREVIOLATION_RESPONSE* pResponse)
        {
        }

        void IFileDialogEvents.OnTypeChange(IFileDialog pfd)
        {
        }

        public unsafe void OnOverwrite(IFileDialog pfd, IShellItem psi, FDE_OVERWRITE_RESPONSE* pResponse)
        {
        }

        void IFileDialogEvents.OnFileOk(IFileDialog pfd)
        {
            OnFileOkEvent?.Invoke(pfd);
        }

        private void ConfigureSelectButton(HWND selectButton, HWND okButton, HWND dialog)
        {
            parentDialog._hSel = selectButton;
            PInvoke.ShowWindow(okButton, SHOW_WINDOW_CMD.SW_HIDE);
            PInvoke.EnableWindow(okButton, false);
            PInvoke.SendMessage(selectButton, PInvoke.BM_SETSTYLE, PInvoke.BS_DEFPUSHBUTTON, (IntPtr)1);

            parentDialog._newWndProcDelegate = parentDialog.NewWndProc;
            parentDialog._originalWndProc = GetWindowLongPtrInternal(dialog, WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC);
            var newWndProcPtr = Marshal.GetFunctionPointerForDelegate(parentDialog._newWndProcDelegate);
            SetWindowLongPtrInternal(dialog, WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC, newWndProcPtr);
        }

        private void ExtractSelectedPaths(IntPtr folderView2Ptr)
        {
            try
            {
                var fv2 = (IFolderView2)Marshal.GetObjectForIUnknown(folderView2Ptr);
                var shellItemArrayGuid = new Guid(COMHelper.IID.SHELL_ITEM_ARRAY);

                fv2.Items(_SVGIO.SVGIO_SELECTION, ref shellItemArrayGuid, out var shellItemArrObj);
                var shellItemArray = (IShellItemArray)shellItemArrObj;

                shellItemArray.GetCount(out var count);
                for (uint i = 0; i < count; i++)
                {
                    shellItemArray.GetItemAt(i, out var item);
                    item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
                    parentDialog.SelectedTargets.Add(path.ToString());
                }
            }
            catch
            {
                // ignore
            }
        }

        private unsafe bool IsSelectButton(HWND h)
        {
            fixed (char* textBufPtr = _textBuffer)
            {
                int textLen = PInvoke.GetWindowText(h, textBufPtr, _textBuffer.Length);
                return textLen == 6 && _textBuffer.AsSpan(0, textLen).SequenceEqual("Select".AsSpan());
            }
        }

        private void ProcessSelectedItems(IFileDialog fd)
        {
            IntPtr pUnk = IntPtr.Zero;
            IntPtr psp = IntPtr.Zero;
            IntPtr ppvPtr = IntPtr.Zero;
            IntPtr folderViewPtr = IntPtr.Zero;
            IntPtr folderView2Ptr = IntPtr.Zero;

            try
            {
                var serviceProviderGuid = new Guid(COMHelper.IID.SERVICE_PROVIDER);
                pUnk = Marshal.GetIUnknownForObject(fd);
                Marshal.QueryInterface(pUnk, ref serviceProviderGuid, out psp);

                var sp = (IServiceProvider)Marshal.GetObjectForIUnknown(psp);
                var folderViewGuid = new Guid(COMHelper.IID.FOLDER_VIEW);
                var folderView2Guid = new Guid(COMHelper.IID.FOLDER_VIEW2);

                sp.QueryService(ref folderViewGuid, ref folderView2Guid, out var ppv);
                ppvPtr = Marshal.GetIUnknownForObject(ppv);

                var fv = (IFolderView)Marshal.GetUniqueObjectForIUnknown(ppvPtr);
                folderViewPtr = Marshal.GetIUnknownForObject(fv);

                var hr = Marshal.QueryInterface(folderViewPtr, ref folderView2Guid, out folderView2Ptr);
                if (hr == 0 && folderView2Ptr != IntPtr.Zero)
                    ExtractSelectedPaths(folderView2Ptr);
            }
            finally
            {
                if (folderView2Ptr != IntPtr.Zero) Marshal.Release(folderView2Ptr);
                if (folderViewPtr != IntPtr.Zero) Marshal.Release(folderViewPtr);
                if (ppvPtr != IntPtr.Zero) Marshal.Release(ppvPtr);
                if (psp != IntPtr.Zero) Marshal.Release(psp);
                if (pUnk != IntPtr.Zero) Marshal.Release(pUnk);
            }

            fd.Close(HRESULT.S_OK);
        }
    }
}

#pragma warning restore