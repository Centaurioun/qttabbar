//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2010  Quizo, Paul Accisano
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using QTPlugin;
using QTPlugin.Interop;

namespace QuizoPlugins {
    internal static class FileOps {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, StringBuilder lpszClass, string lpszWindow);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);


        private static bool fVista = IsVista();

        private const int WM_COMMAND = 0x0111;

        private const bool ENABLE_LOGGER = false;


        public static void FileOperation(FileOpActions action, IntPtr hwndExplr, IShellBrowser shellBrowser) {
            IntPtr hwnd = fVista ? 
                FindWindowEx(hwndExplr, 
                    IntPtr.Zero, 
                    new StringBuilder("ShellTabWindowClass"), 
                    null) : hwndExplr;

            if (fVista)
            {
                // log("SendMessage hwnd " + hwnd );
                SendMessage(hwnd, WM_COMMAND, (IntPtr)action, IntPtr.Zero);
            }
            else
            {
                // log("PostMessage hwnd " + hwnd);
                PostMessage(hwnd, WM_COMMAND, (IntPtr)action, IntPtr.Zero);
            }

            // make selected icons transparent and refresh them immediately.
            // ( when NO-FULLROW-SELECT setting is ON, vista's explorer won't redraw items... )
            if (fVista && action == FileOpActions.Cut && shellBrowser != null)
            {
                RefreshItems(shellBrowser);
            }
                
        }

        private static void RefreshItems(IShellBrowser shellBrowser) {
            IShellView shellView = null;
            IShellFolderView shellFolderView = null;
            // log(" RefreshItems start " );
            try {
                if(0 == shellBrowser.QueryActiveShellView(out shellView)) {
                    shellFolderView = (IShellFolderView)shellView;

                    // no need this?
                    shellFolderView.SetClipboard(true);

                    IntPtr p;
                    int items;
                    int ui;
                    if(0 == shellFolderView.GetSelectedObjects(out p, out items)) {
                        if(p != IntPtr.Zero) {
                            if(items > 0) {
                                IntPtr[] pIDLs = new IntPtr[items];
                                Marshal.Copy(p, pIDLs, 0, items);

                                for(int i = 0; i < items; i++) {
                                    log(" RefreshObject i " + i);
                                    shellFolderView.RefreshObject(pIDLs[i], out ui);
                                }
                            }
                            LocalFree(p);
                        }
                    }
                }
                // log(" RefreshItems end ");
            }
            catch(Exception e) {
                MakeErrorLog(e, "RefreshItems");
            }
            finally {
                // �޸����в���� ���´���ִ��������
                if (shellView != null)
                {
                    // log(" ReleaseComObject shellView " + shellView);
                    // Marshal.ReleaseComObject(shellView);
                }

                if (shellFolderView != null)
                {
                    // log(" ReleaseComObject shellFolderView " + shellFolderView);
                    // Marshal.ReleaseComObject(shellFolderView);
                }
                    
            }
        }


        
        public static void log(string optional)
        {
            if (ENABLE_LOGGER)
            {
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appdataQT = Path.Combine(appdata, "QTTabBar");
                if (!Directory.Exists(appdataQT))
                {
                    Directory.CreateDirectory(appdataQT);
                }
                string path = Path.Combine(appdataQT, "QTTabBarException.log");
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine("[log]" + DateTime.Now.ToString() + " " + optional + "\n");
                    // ��ӡ��������ջ
                    /*
                    var stackTrace = new StackTrace();
                    for (int i = 0; i < stackTrace.FrameCount; i++)
                    {
                        var method = stackTrace.GetFrame(i).GetMethod();
                        writer.WriteLine(
                               "\nmethod ---\n{0}", method);
                    }

                    writer.WriteLine(
                                "\nStackTrace ---\n{0}", stackTrace);
                    */

                    writer.Close();
                }
            }
        }

        public static void MakeErrorLog(Exception ex, string optional = null)
        {
            try
            {
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appdataQT = Path.Combine(appdata, "QTTabBar");
                if (!Directory.Exists(appdataQT))
                {
                    Directory.CreateDirectory(appdataQT);
                }
                string path = Path.Combine(appdataQT, "QTTabBarException.log");
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine(DateTime.Now.ToString());
                    writer.WriteLine(".NET �汾: " + Environment.Version);
                    writer.WriteLine("����ϵͳ�汾: " + Environment.OSVersion.Version + " Major: " + Environment.OSVersion.Version.Major);
                    if (!String.IsNullOrEmpty(optional))
                    {
                        writer.WriteLine("������Ϣ: " + optional);
                    }
                    if (ex == null)
                    {
                        writer.WriteLine("Exception: None");
                        if (Environment.StackTrace != null)
                        {
                            writer.WriteLine(Environment.StackTrace);
                        }
                    }
                    else
                    {
                        writer.WriteLine("\nMessage ---\n{0}", ex.Message);
                        writer.WriteLine(
                            "\nHelpLink ---\n{0}", ex.HelpLink);
                        writer.WriteLine("\nSource ---\n{0}", ex.Source);
                        writer.WriteLine(
                            "\nStackTrace ---\n{0}", ex.StackTrace);
                        writer.WriteLine(
                            "\nTargetSite ---\n{0}", ex.TargetSite);
                    }
                    writer.WriteLine("--------------");
                    writer.WriteLine();
                    writer.Close();
                }
                // SystemSounds.Exclamation.Play();
            }
            catch
            {
            }
            finally
            {
            }
        }

        public static bool MoveSelectedToParent(IPluginServer pluginServer) {
            try {
                Address[] addresses;
                if(!pluginServer.TryGetSelection(out addresses) || addresses.Length == 0)
                    return false;

                string currentPath = pluginServer.SelectedTab.Address.Path;
                string sourcePaths = MakeSourcePaths(addresses);

                if(sourcePaths.Length < 2 || currentPath.Length < 4)
                    return false;

                string targetDir = Path.GetDirectoryName(currentPath);
                if(!String.IsNullOrEmpty(targetDir)) {
                    const int FO_MOVE = 0x0001;
                    const int FO_COPY = 0x0002;
                    const short FOF_ALLOWUNDO = 0x0040;

                    bool fInCompressedFolder = File.Exists(currentPath);		// or unknown virtual folder?? TENUKI here
                    bool fCopy = fInCompressedFolder ^ Control.ModifierKeys == Keys.Control;

                    SHFILEOPSTRUCT sfo = new SHFILEOPSTRUCT();
                    sfo.hwnd = pluginServer.ExplorerHandle;
                    sfo.wFunc = fCopy ? FO_COPY : FO_MOVE;
                    sfo.pFrom = sourcePaths;
                    sfo.pTo = targetDir + "\0";
                    sfo.fFlags = FOF_ALLOWUNDO;

                    SHFileOperation(ref sfo);

                    pluginServer.ExecuteCommand(Commands.RefreshBrowser, null);

                    return true;
                }
            }
            catch {
            }

            return false;
        }

        public static void ShowProperties(IPluginServer pluginServer) {
            const int SEE_MASK_INVOKEIDLIST = 0x0000000c;
            const int SEE_MASK_FLAG_NO_UI = 0x00000400;

            try {
                IntPtr pIDL = PInvoke.CreatePIDL(pluginServer.SelectedTab.Address.ITEMIDLIST);

                if(pIDL != IntPtr.Zero) {
                    SHELLEXECUTEINFO sei = new SHELLEXECUTEINFO();
                    sei.cbSize = Marshal.SizeOf(sei);
                    sei.fMask = SEE_MASK_INVOKEIDLIST | SEE_MASK_FLAG_NO_UI;
                    sei.lpVerb = "properties";
                    sei.lpIDList = pIDL;

                    ShellExecuteEx(ref sei);

                    Marshal.FreeCoTaskMem(pIDL);
                }
            }
            catch {
            }
        }

        private static string MakeSourcePaths(Address[] addresses) {
            string str = String.Empty;
            foreach(Address ad in addresses) {
                if(!String.IsNullOrEmpty(ad.Path) /*&& ( File.Exists( ad.Path ) || Directory.Exists( ad.Path ) ) */)	//to allow compressed..
                    str += ad.Path + "\0";
            }
            str += "\0";

            return str;
        }

        private static bool IsVista() {
            return Environment.OSVersion.Version.Major > 5;
        }

    }

    enum FileOpActions {
        Delete = 0x7011,
        Cut = 0x7018,
        Copy = 0x7019,
        Paste = 0x701A,
        Undo = 0x701B,
        CopyTo = 0x701E,
        MoveTo = 0x701F,
        Redo = 0x7020,
        Properties = 0x7013,

        //PasteShortcut		= 0x701C
        //SelectAll			= 0x7021,
        //InvertSelection	= 0x7022,
        //Customize folder	= 0x7032,
        //FolderOptions		= 0xA123, ??
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("37A378C0-F82D-11CE-AE65-08002B2E1262")]
    interface IShellFolderView {
        [PreserveSig]
        int Rearrange(IntPtr lParamSort);
        [PreserveSig]
        int GetArrangeParam(out IntPtr plParamSort);
        void ArrangeGrid();
        void AutoArrange();
        void GetAutoArrange();
        [PreserveSig]
        int AddObject(IntPtr pidl, out int puItem);
        [PreserveSig]
        int GetObject(out IntPtr ppidl, int uItem);
        [PreserveSig]
        int RemoveObject(IntPtr pidl, out int puItem);
        void GetObjectCount(out int puCount);
        void SetObjectCount(int uCount, int dwFlags);
        void UpdateObject(IntPtr pidlOld, IntPtr pidlNew, out int puItem);
        [PreserveSig]
        int RefreshObject(IntPtr pidl, out int puItem);
        [PreserveSig]
        int SetRedraw(bool bRedraw);
        void GetSelectedCount(out int puSelected);
        [PreserveSig]
        int GetSelectedObjects(out IntPtr pppidl, out int puItems);
        void IsDropOnSource(IntPtr pDropTarget);
        void GetDragPoint(ref POINT ppt);
        void GetDropPoint(ref POINT ppt);
        void MoveIcons(IntPtr pDataObject);
        void SetItemPos(IntPtr pidl, ref POINT ppt);
        void IsBkDropTarget(IntPtr pDropTarget);
        [PreserveSig]
        int SetClipboard(bool bMove);
        void SetPoints(ref IntPtr pDataObject);
        void GetItemSpacing(IntPtr pSpacing);
        void SetCallback(IntPtr pNewCB, out IntPtr ppOldCB);
        void Select(int dwFlags);
        void QuerySupport(IntPtr pdwSupport);
        void SetAutomationObject(IntPtr pdisp);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct SHFILEOPSTRUCT {
        public IntPtr hwnd;
        public int wFunc;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pFrom;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string pTo;
        public short fFlags;							//typedef WORD FILEOP_FLAGS;
        public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpszProgressTitle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    internal struct SHELLEXECUTEINFO {
        public int cbSize;
        public int fMask;
        public IntPtr hwnd;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpVerb;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpFile;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpParameters;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        public IntPtr lpClass;
        public IntPtr hkeyClass;
        public int dwHotKey;
        public IntPtr hIconhMonitor;
        public IntPtr hProcess;
    }
}
