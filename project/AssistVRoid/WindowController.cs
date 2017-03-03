using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

using System.Drawing;

namespace MyLauncher
{
    public class WindowControllerManager
    {
        static WindowControllerManager _window_system = null;
        static Object top_ws_list_lock = new Object();
        WindowController top_ws = null;
        List<WindowController> top_ws_list = new List<WindowController>();
        List<WindowController> ws_list = new List<WindowController>();

        public WindowControllerManager(string title)
        {
            _window_system = this;
            top_ws = GetTopWindowStatusList(title); // titleが一致するウィンドウを探す

            if (top_ws == null) return;

            // 子ウィンドウをサブ含めて収集する
            {
                var ws = new WindowController(top_ws.hWnd);
                ws_list.Add(ws);
                EnumChildWindows(ws.hWnd, EnumWindowProc, default(IntPtr));
            }

            // 親子関係を把握する
            foreach (var ws in ws_list)
            {
                var top_chiled_h = GetWindow(ws.hWnd, (UInt32)GW.GW_CHILD);
                if ((uint)top_chiled_h!=0)
                {
                    var chiled_h = top_chiled_h;
                    while ((uint)chiled_h != 0)
                    {
                        foreach (var ws2 in ws_list)
                        {
                            if ( ws2.hWnd == chiled_h)
                            {
                                ws2.owner = ws;
                                break;
                            }
                        }
                        chiled_h = GetWindow(chiled_h, (UInt32)GW.GW_HWNDNEXT);
                    }
                }

            }
        }

        public void Dump_A()
        {
            foreach ( var ws in ws_list )
            {
                if (ws.owner != null)
                {
                    Console.WriteLine("{0} {1} {2} {3}", ws.hWnd, ws.text, ws.class_name, ws.owner.hWnd);
                }
                else
                {
                    Console.WriteLine("{0} {1} {2}", ws.hWnd, ws.text, ws.class_name);
                }
            }
        }

        public bool IsGetTop()
        {
            if (top_ws == null) return false;
            return true;
        }

        public WindowController GetTop()
        {
            return top_ws;
        }

        public WindowController GetChiledByText(string text, int index = 0)
        {
            var i = 0;
            foreach (var ws in ws_list)
            {
                if (ws.text == text)
                {
                    if (i==index) return ws;
                    i++;
                }
            }
            return null;
        }

        public WindowController GetChiledByClassName(string class_name, int index = 0)
        {
            var i = 0;
            foreach (var ws in ws_list)
            {
                if (ws.class_name == class_name)
                {
                    if (i == index) return ws;
                    i++;
                }
            }
            return null;
        }

        // ClassNameに指定された文字列が含まれていれば良い
        public WindowController GetChiledByClassNameNear(string class_name_near, int index = 0)
        {
            var i = 0;
            foreach (var ws in ws_list)
            {
                if (ws.class_name.IndexOf( class_name_near) >= 0)
                {
                    if (i == index) return ws;
                    i++;
                }
            }
            return null;
        }

        public WindowController GetChiledByIndex( int index)
        {
            var i = 0;
            foreach (var ws in ws_list)
            {
                if (i == index) return ws;
                i++;
            }
            return null;
        }


        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal delegate bool EnumWindowsProcDelegate(IntPtr windowHandle, IntPtr lParam);


        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public extern static bool EnumWindows(EnumWindowsDelegate lpEnumFunc, IntPtr lparam);
        public delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lparam);

        [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(
            IntPtr handle,
            [MarshalAs(UnmanagedType.FunctionPtr)] EnumWindowsProcDelegate enumProc,
            IntPtr lParam);

        enum GW
        {
            GW_CHILD = 5,
            GW_HWNDNEXT = 2,
            GW_OWNER = 4,
        };

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, System.UInt32 uCmd);

        //[DllImport("user32.dll")]
        //private static extern IntPtr GetForegroundWindow();

        internal bool EnumWindowProc(IntPtr handle, IntPtr lParam)
        {
            var ws = new WindowController(handle);
            ws_list.Add(ws);
            return true;
        }

        private static WindowController GetTopWindowStatusList(string title)
        {
            EnumWindows(new EnumWindowsDelegate(EnumWindowCallBack), IntPtr.Zero);

            //すべてのプロセスを列挙する
            lock (top_ws_list_lock)
            {
                foreach (var ws in _window_system.top_ws_list)
                {
                    //指定された文字列がメインウィンドウのタイトルに含まれているか調べる
                    if (ws.text.IndexOf(title) != -1)
                    {
                        return ws;
                    }
                }
            }
            return null;
        }

        private static bool EnumWindowCallBack(IntPtr hWnd, IntPtr lparam)
        {
            lock (top_ws_list_lock)
            {
                if (_window_system != null)
                {
                    var ws = new WindowController(hWnd);
                    _window_system.top_ws_list.Add(ws);
                }
            }
            return true;
        }
    }


    public class WindowController
    {
        public IntPtr hWnd;
        public string text;
        public string class_name;
        public WindowController owner = null;
        const int STRING_MAX = 1024;

        //enum GW
        //{
        //    GW_OWNER = 4,
        //};

        public WindowController(IntPtr _hWnd)
        {
            hWnd = _hWnd;
            var _text = new StringBuilder(STRING_MAX);
            var _class_name = new StringBuilder(STRING_MAX);
            GetWindowText(hWnd, _text, STRING_MAX);
            GetClassName(hWnd, _class_name, STRING_MAX);

            text = _text.ToString();
            class_name = _class_name.ToString();
        }

        public WindowController GetOwner()
        {
            //var owner_hWnd = GetWindow(hWnd, (UInt32)GW.GW_OWNER);
            //var ws = new WindowController(owner_hWnd);
            return owner;
        }

        public void SendText( string text )
        {
            var sb_text = new StringBuilder(text);
            SendMessage( hWnd, WM_SETTEXT, 0, sb_text);
            //PostMessage( hWnd, WM_SETTEXT, 0, sb_text);
        }

        public void ClickL()
        {
            SendMessage(hWnd, __BM_CLICK, 0, 0);
            //PostMessage(hWnd, __BM_CLICK, 0, 0);
        }

        public bool IsActive()
        {
            var h = GetForegroundWindow();
            if (h == hWnd) return true;
            return false;
        }

        public Rectangle GetRectange()
        {
            var rc = new RECT();
            GetWindowRect(hWnd, out rc);

            var r = new Rectangle(rc.left, rc.top, rc.right - rc.left, rc.bottom - rc.top);
            return r;
        }

        public void Active()
        {
            SetForegroundWindow(hWnd);
        }

        public void ClickL( int x, int y )
        {
            UInt16 rrx = (UInt16)(x);
            UInt16 rry = (UInt16)(y);
            if (rrx < 0) rrx = 0;
            if (rry < 0) rry = 0;
            UInt32 pos = ((UInt32)rry << 16) | (UInt32)rrx;
            //IntPtr ph = p.MainWindowHandle;
            //IntPtr ph = (IntPtr)background_target_window_handle;

            //マウスメッセージを送り付けてみる
            SendMessageU(hWnd, (uint)WM.WM_MOUSEMOVE, new IntPtr((int)MK.None), new IntPtr(pos));
            WaitSleep.Do(100);
            SendMessageU(hWnd, (uint)WM.WM_LBUTTONDOWN, new IntPtr((int)MK.MK_LBUTTON), new IntPtr(pos));
            WaitSleep.Do(100);
            SendMessageU(hWnd, (uint)WM.WM_LBUTTONUP, new IntPtr((int)MK.None), new IntPtr(pos));
        }

        private const int MOUSEEVENTF_MOVE = 0x1;           // マウスを移動する
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000;    // 絶対座標指定
        private const int MOUSEEVENTF_LEFTDOWN = 0x2;
        private const int MOUSEEVENTF_LEFTUP = 0x4;
        public void ClickLActive(int x, int y)
        {
            var rc = GetRectange();
            x += rc.X;
            y += rc.Y;

            WaitSleep.Do(100);
            SetCursorPos(x, y);                             // button2へ移動
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);              // マウスの左ボタンダウンイベントを発生させる
            WaitSleep.Do(100);
            SetCursorPos(x, y);                             // button2へ移動
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);                // マウスの左ボタンアップイベント
        }

        public void MouseMove( int x, int y)
        {
            var rc = GetRectange();

            UInt16 rrx = (UInt16)(x+rc.X);
            UInt16 rry = (UInt16)(y+rc.Y);
            if (rrx < 0) rrx = 0;
            if (rry < 0) rry = 0;
            UInt32 pos = ((UInt32)rry << 16) | (UInt32)rrx;
            //IntPtr ph = p.MainWindowHandle;
            //IntPtr ph = (IntPtr)background_target_window_handle;

            //マウスメッセージを送り付けてみる
            SendMessageU(hWnd, (uint)WM.WM_MOUSEMOVE, new IntPtr((int)MK.None), new IntPtr(pos));
            WaitSleep.Do(100);

        }

        #region win32api_DLL_Import
        // SendMessage
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern Int32 SendMessage(IntPtr hWnd, Int32 Msg, Int32 wParam, StringBuilder lParam);
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern Int32 SendMessage(IntPtr hWnd, Int32 Msg, Int32 wParam, Int32 lParam);
        [DllImport("User32.dll", EntryPoint = "SendMessage")]
        private static extern Int32 SendMessageU(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32", EntryPoint = "PostMessage")]
        private static extern Int32 PostMessage(IntPtr hWnd, Int32 Msg, Int32 wParam, StringBuilder lParam);
        [DllImport("user32", EntryPoint = "PostMessage")]
        private static extern Int32 PostMessage(IntPtr hWnd, Int32 Msg, Int32 wParam, Int32 lParam);

        // SendMessageのコマンド
        public const Int32 WM_COPYDATA = 0x4A;
        public const Int32 WM_USER = 0x400;
        public const Int32 WM_SETTEXT = 0x0c;
        public const Int32 __BM_CLICK = 0xF5;
        // どうにかしてアクティブにしたい…ハンドル取れたら十分なんですけど
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool BringWindowToTop(IntPtr hWnd);
        #endregion

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int length);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder className, int maxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, System.UInt32 uCmd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("User32.Dll")]
        static extern int GetWindowRect(
            IntPtr hWnd,      // ウィンドウのハンドル
            out RECT rect   // ウィンドウの座標値
            );

        private enum WM : uint
        {
            WM_MOUSEMOVE   = 0x200,
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP   = 0x0202,
        }
        private enum MK
        {
            None = 0,
            MK_LBUTTON = 0x0001,
        }

        // マウス制御
        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void SetCursorPos(int X, int Y);

        [DllImport("USER32.dll", CallingConvention = CallingConvention.StdCall)]
        static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);
    }
}
