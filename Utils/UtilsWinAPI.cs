using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace LuckyStars.Utils
{
    /// <summary>
    /// Windows API封装类，提供对Windows API的访问
    /// </summary>
    public static class WinAPI
    {
        #region 窗口相关

        /// <summary>
        /// 窗口样式
        /// </summary>
        [Flags]
        public enum WindowStyles : uint
        {
            WS_OVERLAPPED = 0x00000000,
            WS_POPUP = 0x80000000,
            WS_CHILD = 0x40000000,
            WS_MINIMIZE = 0x20000000,
            WS_VISIBLE = 0x10000000,
            WS_DISABLED = 0x08000000,
            WS_CLIPSIBLINGS = 0x04000000,
            WS_CLIPCHILDREN = 0x02000000,
            WS_MAXIMIZE = 0x01000000,
            WS_CAPTION = 0x00C00000,
            WS_BORDER = 0x00800000,
            WS_DLGFRAME = 0x00400000,
            WS_VSCROLL = 0x00200000,
            WS_HSCROLL = 0x00100000,
            WS_SYSMENU = 0x00080000,
            WS_THICKFRAME = 0x00040000,
            WS_GROUP = 0x00020000,
            WS_TABSTOP = 0x00010000,
            WS_MINIMIZEBOX = 0x00020000,
            WS_MAXIMIZEBOX = 0x00010000
        }

        /// <summary>
        /// 扩展窗口样式
        /// </summary>
        [Flags]
        public enum WindowStylesEx : uint
        {
            WS_EX_DLGMODALFRAME = 0x00000001,
            WS_EX_NOPARENTNOTIFY = 0x00000004,
            WS_EX_TOPMOST = 0x00000008,
            WS_EX_ACCEPTFILES = 0x00000010,
            WS_EX_TRANSPARENT = 0x00000020,
            WS_EX_MDICHILD = 0x00000040,
            WS_EX_TOOLWINDOW = 0x00000080,
            WS_EX_WINDOWEDGE = 0x00000100,
            WS_EX_CLIENTEDGE = 0x00000200,
            WS_EX_CONTEXTHELP = 0x00000400,
            WS_EX_RIGHT = 0x00001000,
            WS_EX_LEFT = 0x00000000,
            WS_EX_RTLREADING = 0x00002000,
            WS_EX_LTRREADING = 0x00000000,
            WS_EX_LEFTSCROLLBAR = 0x00004000,
            WS_EX_RIGHTSCROLLBAR = 0x00000000,
            WS_EX_CONTROLPARENT = 0x00010000,
WS_EX_STATICSEDGE = 0x00020000,
            WS_EX_APPWINDOW = 0x00040000,
            WS_EX_LAYERED = 0x00080000,
            WS_EX_NOINHERITLAYOUT = 0x00100000,
            WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
            WS_EX_LAYOUTRTL = 0x00400000,
            WS_EX_COMPOSITED = 0x02000000,
            WS_EX_NOACTIVATE = 0x08000000
        }

        /// <summary>
        /// 窗口消息
        /// </summary>
        public enum WindowMessages : uint
        {
            WM_NULL = 0x0000,
            WM_CREATE = 0x0001,
            WM_DESTROY = 0x0002,
            WM_MOVE = 0x0003,
            WM_SIZE = 0x0005,
            WM_ACTIVATE = 0x0006,
            WM_SETFOCUS = 0x0007,
            WM_KILLFOCUS = 0x0008,
            WM_ENABLE = 0x000A,
            WM_SETREDRAW = 0x000B,
            WM_SETTEXT = 0x000C,
            WM_GETTEXT = 0x000D,
            WM_GETTEXTLENGTH = 0x000E,
            WM_PAINT = 0x000F,
            WM_CLOSE = 0x0010,
            WM_QUIT = 0x0012,
            WM_ERASEBKGND = 0x0014,
            WM_SHOWWINDOW = 0x0018,
            WM_NCCREATE = 0x0081,
            WM_NCDESTROY = 0x0082,
            WM_NCCALCSIZE = 0x0083,
            WM_NCHITTEST = 0x0084,
            WM_NCPAINT = 0x0085,
            WM_NCACTIVATE = 0x0086,
            WM_KEYDOWN = 0x0100,
            WM_KEYUP = 0x0101,
            WM_SYSKEYDOWN = 0x0104,
            WM_SYSKEYUP = 0x0105,
            WM_SYSCOMMAND = 0x0112,
            WM_TIMER = 0x0113,
            WM_HSCROLL = 0x0114,
            WM_VSCROLL = 0x0115,
            WM_INITMENUPOPUP = 0x0117,
            WM_COMMAND = 0x0111,
            WM_DEVICECHANGE = 0x0219,
            WM_DISPLAYCHANGE = 0x007E,
            WM_DPICHANGED = 0x02E0,
            WM_SETTINGCHANGE = 0x001A,
            WM_THEMECHANGED = 0x031A,
            WM_DROPFILES = 0x0233,
            WM_DWMCOMPOSITIONCHANGED = 0x031E,
            WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320,
            WM_POWERBROADCAST = 0x0218,
            WM_COPYDATA = 0x004A,
            WM_HOTKEY = 0x0312
        }

        /// <summary>
        /// 窗口消息结果常量
        /// </summary>
        public static readonly IntPtr HWND_BROADCAST = new IntPtr(0xffff);
        public const uint HWND_TOP = 0;
        public const uint HWND_BOTTOM = 1;
        public const uint HWND_TOPMOST = 0xFFFFFFFF;
        public const uint HWND_NOTOPMOST = 0xFFFFFFFE;
        public const uint HWND_MESSAGE = 0xFFFFFFFC;

        /// <summary>
        /// 窗口位置标志
        /// </summary>
        [Flags]
        public enum SetWindowPosFlags : uint
        {
            SWP_ASYNCWINDOWPOS = 0x4000,
            SWP_DEFERERASE = 0x2000,
            SWP_DRAWFRAME = 0x0020,
            SWP_FRAMECHANGED = 0x0020,
            SWP_HIDEWINDOW = 0x0080,
            SWP_NOACTIVATE = 0x0010,
            SWP_NOCOPYBITS = 0x0100,
            SWP_NOMOVE = 0x0002,
            SWP_NOOWNERZORDER = 0x0200,
            SWP_NOREDRAW = 0x0008,
            SWP_NOREPOSITION = 0x0200,
            SWP_NOSENDCHANGING = 0x0400,
            SWP_NOSIZE = 0x0001,
            SWP_NOZORDER = 0x0004,
            SWP_SHOWWINDOW = 0x0040
        }

        /// <summary>
        /// 窗口长整型索引
        /// </summary>
        public enum WindowLongFlags : int
        {
            GWL_EXSTYLE = -20,
            GWLP_HINSTANCE = -6,
            GWLP_HWNDPARENT = -8,
            GWL_ID = -12,
            GWL_STYLE = -16,
            GWL_USERDATA = -21,
            GWL_WNDPROC = -4,
            DWLP_USER = 0x8,
            DWLP_MSGRESULT = 0x0,
            DWLP_DLGPROC = 0x4
        }

        /// <summary>
        /// 窗口显示状态
        /// </summary>
        public enum ShowWindowCommands
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_FORCEMINIMIZE = 11
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public RECT(Rectangle rect)
            {
                Left = rect.Left;
                Top = rect.Top;
                Right = rect.Right;
                Bottom = rect.Bottom;
            }

            public Rectangle ToRectangle()
            {
                return new Rectangle(Left, Top, Right - Left, Bottom - Top);
            }

            public int Width
            {
                get { return Right - Left; }
            }

            public int Height
            {
                get { return Bottom - Top; }
            }

            public override string ToString()
            {
                return $"{{Left={Left},Top={Top},Right={Right},Bottom={Bottom}}}";
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommands nCmdShow);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, WindowLongFlags nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, WindowLongFlags nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, WindowLongFlags nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, WindowLongFlags nIndex, int dwNewLong);

        /// <summary>
        /// 获取窗口长整型属性
        /// </summary>
        public static IntPtr GetWindowLong(IntPtr hWnd, WindowLongFlags nIndex)
        {
            if (IntPtr.Size == 8)
                return GetWindowLongPtr64(hWnd, nIndex);
            else
                return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        /// <summary>
        /// 设置窗口长整型属性
        /// </summary>
        public static IntPtr SetWindowLong(IntPtr hWnd, WindowLongFlags nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        /// <summary>
        /// 获取窗口标题
        /// </summary>
        public static string GetWindowTitle(IntPtr hWnd)
        {
            try
            {
                int length = GetWindowTextLength(hWnd);
                if (length == 0)
                    return string.Empty;

                StringBuilder builder = new StringBuilder(length + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取窗口标题失败: {ex.Message}");
                return string.Empty;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion 窗口相关

        #region 壁纸相关

        /// <summary>
        /// 壁纸风格
        /// </summary>
        public enum WallpaperStyle : int
        {
            /// <summary>
            /// 居中
            /// </summary>
            Center = 0,
            /// <summary>
            /// 平铺
            /// </summary>
            Tile = 1,
            /// <summary>
            /// 拉伸
            /// </summary>
            Stretch = 2,
            /// <summary>
            /// 适应
            /// </summary>
            Fit = 3,
            /// <summary>
            /// 填充
            /// </summary>
            Fill = 4,
            /// <summary>
            /// 跨区
            /// </summary>
            Span = 22
        }

        /// <summary>
        /// WorkerW 查找回调
        /// </summary>
        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        /// <summary>
        /// 查找所有顶级窗口
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        /// <summary>
        /// 查找指定窗口的子窗口
        /// </summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        /// <summary>
        /// 获取类名
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        /// <summary>
        /// 查找桌面窗口句柄（Progman 或 WorkerW）
        /// </summary>
        public static IntPtr FindDesktopWindowHandle()
        {
            // 首先尝试找到 Progman 窗口
            IntPtr progman = FindWindow("Progman", null);
            if (progman == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            // 向Progman发送消息以创建WorkerW
            SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero, 0, 1000, out _);

            // 查找WorkerW窗口
            List<IntPtr> workerWs = new List<IntPtr>();
            EnumWindows((wnd, param) =>
            {
                StringBuilder className = new StringBuilder(256);
                GetClassName(wnd, className, className.Capacity);
                if (className.ToString() == "WorkerW")
                {
                    IntPtr shellDllDefView = FindWindowEx(wnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (shellDllDefView != IntPtr.Zero)
                    {
                        workerWs.Add(wnd);
                    }
                }
                return true;
            }, IntPtr.Zero);

            if (workerWs.Count > 0)
            {
                return workerWs[0];
            }

            // 如果找不到WorkerW，则返回Progman
            return progman;
        }

        /// <summary>
        /// 查找壁纸容器窗口
        /// </summary>
        public static IntPtr FindWallpaperContainerWindow()
        {
            IntPtr desktopWnd = FindDesktopWindowHandle();
            if (desktopWnd == IntPtr.Zero)
                return IntPtr.Zero;

            // 查找 SHELLDLL_DefView
            IntPtr shellDefView = FindWindowEx(desktopWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellDefView == IntPtr.Zero)
                return IntPtr.Zero;

            // 查找 SysListView32
            IntPtr folderView = FindWindowEx(shellDefView, IntPtr.Zero, "SysListView32", "FolderView");
            return folderView;
        }

        /// <summary>
        /// 系统参数信息，壁纸相关
        /// </summary>
        private const int SPI_GETDESKWALLPAPER = 0x0073;
        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPI_GETDESKWALLPAPERSTYLE = 0x0213;
        private const int SPI_SETDESKWALLPAPERSTYLE = 0x0214;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SystemParametersInfo(uint uiAction, uint uiParam, StringBuilder pvParam, uint fWinIni);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SystemParametersInfo(uint uiAction, uint uiParam, ref int pvParam, uint fWinIni);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SystemParametersInfo(uint uiAction, uint uiParam, string pvParam, uint fWinIni);

        /// <summary>
        /// 获取系统壁纸路径
        /// </summary>
        public static string GetSystemWallpaperPath()
        {
            try
            {
                StringBuilder wallpaperPath = new StringBuilder(260);
                if (SystemParametersInfo(SPI_GETDESKWALLPAPER, (uint)wallpaperPath.Capacity, wallpaperPath, 0) != 0)
                {
                    return wallpaperPath.ToString();
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取系统壁纸路径失败: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 设置系统壁纸
        /// </summary>
        public static bool SetSystemWallpaper(string path, WallpaperStyle style)
        {
            try
            {
                if (!File.Exists(path))
                    return false;

                // 设置壁纸样式
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
                {
                    if (key != null)
                    {
                        switch (style)
                        {
                            case WallpaperStyle.Tile:
                                key.SetValue("TileWallpaper", "1");
                                key.SetValue("WallpaperStyle", "0");
                                break;
                            case WallpaperStyle.Center:
                                key.SetValue("TileWallpaper", "0");
                                key.SetValue("WallpaperStyle", "0");
                                break;
                            case WallpaperStyle.Stretch:
                                key.SetValue("TileWallpaper", "0");
                                key.SetValue("WallpaperStyle", "2");
                                break;
                            case WallpaperStyle.Fit:
                                key.SetValue("TileWallpaper", "0");
                                key.SetValue("WallpaperStyle", "6");
                                break;
                            case WallpaperStyle.Fill:
                                key.SetValue("TileWallpaper", "0");
                                key.SetValue("WallpaperStyle", "10");
                                break;
                            case WallpaperStyle.Span:
                                key.SetValue("TileWallpaper", "0");
                                key.SetValue("WallpaperStyle", "22");
                                break;
                        }
                    }
                }

                // 设置壁纸路径
                int result = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                return result != 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置系统壁纸失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送超时消息
        /// </summary>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        #endregion 壁纸相关

        #region DWM相关
        
        /// <summary>
        /// DWM边框颜色
        /// </summary>
        public enum DWMNCRENDERINGPOLICY
        {
            DWMNCRP_USEWINDOWSTYLE,
            DWMNCRP_DISABLED,
            DWMNCRP_ENABLED,
            DWMNCRP_LAST
        }

        /// <summary>
        /// DWM窗口属性
        /// </summary>
        public enum DWMWINDOWATTRIBUTE
        {
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            DWMWA_CLOAK,
            DWMWA_CLOAKED,
            DWMWA_FREEZE_REPRESENTATION,
            DWMWA_PASSIVE_UPDATE_MODE,
            DWMWA_USE_HOSTBACKDROPBRUSH,
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,
            DWMWA_BORDER_COLOR,
            DWMWA_CAPTION_COLOR,
            DWMWA_TEXT_COLOR,
            DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,
            DWMWA_SYSTEMBACKDROP_TYPE,
            DWMWA_LAST
        }

        /// <summary>
        /// DWM角属性
        /// </summary>
        public enum DWM_WINDOW_CORNER_PREFERENCE
        {
            DWMWCP_DEFAULT = 0,
            DWMWCP_DONOTROUND = 1,
            DWMWCP_ROUND = 2,
            DWMWCP_ROUNDSMALL = 3
        }

        /// <summary>
        /// DWM系统背景类型
        /// </summary>
        public enum DWM_SYSTEMBACKDROP_TYPE
        {
            DWMSBT_AUTO = 0,
            DWMSBT_NONE = 1,
            DWMSBT_MAINWINDOW = 2,
            DWMSBT_TRANSIENTWINDOW = 3,
            DWMSBT_TABBEDWINDOW = 4
        }

        [DllImport("dwmapi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll")]
        public static extern int DwmIsCompositionEnabled(out bool enabled);

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;

            public MARGINS(int all) : this(all, all, all, all) { }

            public MARGINS(int left, int right, int top, int bottom)
            {
                cxLeftWidth = left;
                cxRightWidth = right;
                cyTopHeight = top;
                cyBottomHeight = bottom;
            }
        }

        /// <summary>
        /// 设置窗口的圆角属性
        /// </summary>
        public static bool SetWindowCornerPreference(IntPtr hwnd, DWM_WINDOW_CORNER_PREFERENCE cornerPreference)
        {
            if (!IsWindows11OrNewer())
                return false;

            try
            {
                int preference = (int)cornerPreference;
                int result = DwmSetWindowAttribute(
                    hwnd,
                    DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE,
                    ref preference,
                    sizeof(int));
                return result == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置窗口圆角失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置窗口的系统背景类型
        /// </summary>
        public static bool SetWindowSystemBackdropType(IntPtr hwnd, DWM_SYSTEMBACKDROP_TYPE backdropType)
        {
            if (!IsWindows11OrNewer())
                return false;

            try
            {
                int backdrop = (int)backdropType;
                int result = DwmSetWindowAttribute(
                    hwnd,
                    DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                    ref backdrop,
                    sizeof(int));
                return result == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置窗口背景类型失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置窗口深色主题
        /// </summary>
        public static bool SetWindowDarkMode(IntPtr hwnd, bool darkMode)
        {
            if (!IsWindows10OrNewer())
                return false;

            try
            {
                int darkModeValue = darkMode ? 1 : 0;
                int result = DwmSetWindowAttribute(
                    hwnd,
                    DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                    ref darkModeValue,
                    sizeof(int));
                return result == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置窗口深色模式失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 将窗口帧扩展到客户区
        /// </summary>
        public static bool ExtendFrameIntoClientArea(IntPtr hwnd, MARGINS margins)
        {
            try
            {
                int result = DwmExtendFrameIntoClientArea(hwnd, ref margins);
                return result == 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"扩展窗口帧到客户区失败: {ex.Message}");
                return false;
            }
        }

        #endregion DWM相关

        #region 检测系统版本

        /// <summary>
        /// 判断系统是否为Windows 10及以上版本
        /// </summary>
        public static bool IsWindows10OrNewer()
        {
            return Environment.OSVersion.Version.Major >= 10;
        }

        /// <summary>
        /// 判断系统是否为Windows 11及以上版本
        /// </summary>
        public static bool IsWindows11OrNewer()
        {
            return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000;
        }

        #endregion 检测系统版本

        #region 系统电源管理

        /// <summary>
        /// 电源状态变更事件类型
        /// </summary>
        public enum PowerBroadcastType : uint
        {
            PBT_APMPOWERSTATUSCHANGE = 0x000A,
            PBT_APMRESUMEAUTOMATIC = 0x0012,
            PBT_APMRESUMECRITICAL = 0x0006,
            PBT_APMRESUMESUSPEND = 0x0007,
            PBT_APMSUSPEND = 0x0004,
            PBT_POWERSETTINGCHANGE = 0x8013,
            PBT_APMBATTERYLOW = 0x0009,
            PBT_APMOEMEVENT = 0x000B,
            PBT_APMQUERYSUSPEND = 0x0000,
            PBT_APMQUERYSUSPENDFAILED = 0x0002,
            PBT_APMRESUMESWITCHBAT = 0x0DCC
        }

        /// <summary>
            /// 电源方案GUID定义
            /// </summary>
            public static class PowerSchemeGuids
            {
                // 电源方案
                public static readonly Guid GUID_MAX_POWER_SAVINGS = new Guid("a1841308-3541-4fab-bc81-f71556f20b4a");      // 节能
                public static readonly Guid GUID_MIN_POWER_SAVINGS = new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");      // 高性能
                public static readonly Guid GUID_TYPICAL_POWER_SAVINGS = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");   // 平衡

                // 电源功能设置
                public static readonly Guid GUID_HIBERNATE_FASTS = new Guid("94ac6d29-73ce-41a6-809f-6363ba21b47e");        // 休眠后快速启动
                public static readonly Guid GUID_ALLOW_DISPLAY_REQUIRED = new Guid("a9ceb8da-cd46-44fb-a98b-02af69de4623");  // 允许显示器休眠
                public static readonly Guid GUID_ALLOW_AWAYMODE = new Guid("25dfa149-5dd1-4736-b5ab-e8a37b5b8187");         // 允许离开模式
                public static readonly Guid GUID_SYSTEM_BUTTON_SUBGROUP = new Guid("4f971e89-eebd-4455-a8de-9e59040e7347");  // 电源按钮设置
                public static readonly Guid GUID_POWERBUTTON_ACTION = new Guid("7648efa3-dd9c-4e3e-b566-50f929386280");      // 电源按钮操作
                public static readonly Guid GUID_SLEEPBUTTON_ACTION = new Guid("96996bc0-ad50-47ec-923b-6f41874dd9eb");      // 睡眠按钮操作
                public static readonly Guid GUID_LIDCLOSE_ACTION = new Guid("5ca83367-6e45-459f-a27b-476b1d01c936");         // 盖子关闭操作
            }
        }

        /// <summary>
        /// 设置/取消显示器待机禁用
        /// </summary>
        public static bool SetDisplayRequired(bool required)
        {
            try
            {
                if (required)
                {
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
                }
                else
                {
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置显示器电源状态失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置/取消系统空闲待机禁用
        /// </summary>
        public static bool SetSystemRequired(bool required)
        {
            try
            {
                if (required)
                {
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED);
                }
                else
                {
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置系统电源状态失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置/取消显示器和系统待机禁用
        /// </summary>
        public static bool SetDisplayAndSystemRequired(bool required)
        {
            try
            {
                if (required)
                {
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);
                }
                else
                {
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
                }
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置系统和显示器电源状态失败: {ex.Message}");
                return false;
            }
        }

        [Flags]
        private enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        #endregion 系统电源管理

        #region 多显示器相关

        /// <summary>
        /// 显示器信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        /// <summary>
        /// 显示器信息扩展
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFOEX
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        /// <summary>
        /// 物理显示器信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }

        /// <summary>
        /// 显示设备
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DISPLAY_DEVICE
        {
            public uint cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public uint StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        // 显示设备状态标志
        public const uint DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x00000001;
        public const uint DISPLAY_DEVICE_PRIMARY_DEVICE = 0x00000004;
        public const uint DISPLAY_DEVICE_MIRRORING_DRIVER = 0x00000008;
        public const uint DISPLAY_DEVICE_ACTIVE = 0x00000001;

        // 显示器枚举回调委托
        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("dxva2.dll", EntryPoint = "GetNumberOfPhysicalMonitorsFromHMONITOR")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, ref uint pdwNumberOfPhysicalMonitors);

        [DllImport("dxva2.dll", EntryPoint = "GetPhysicalMonitorsFromHMONITOR")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", EntryPoint = "DestroyPhysicalMonitors")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        // 显示器常量
        public const uint MONITORINFOF_PRIMARY = 0x00000001;
        public const uint MONITOR_DEFAULTTONULL = 0x00000000;
        public const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;
        public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        public struct POINT
        {
            public int x;
            public int y;

            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        /// <summary>
        /// 获取指定点所在的显示器句柄
        /// </summary>
        public static IntPtr GetMonitorFromPoint(int x, int y, uint flags = MONITOR_DEFAULTTONEAREST)
        {
            POINT pt = new POINT(x, y);
            return MonitorFromPoint(pt, flags);
        }

        /// <summary>
        /// 获取指定窗口所在的显示器句柄
        /// </summary>
        public static IntPtr GetMonitorFromWindow(IntPtr hwnd, uint flags = MONITOR_DEFAULTTONEAREST)
        {
            return MonitorFromWindow(hwnd, flags);
        }

        /// <summary>
        /// 获取显示器信息
        /// </summary>
        public static bool GetMonitorInfoEx(IntPtr hMonitor, out MONITORINFOEX info)
        {
            info = new MONITORINFOEX();
            info.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFOEX));
            return GetMonitorInfo(hMonitor, ref info);
        }

        /// <summary>
        /// 获取所有显示监视器
        /// </summary>
        public static List<MONITORINFOEX> GetAllMonitors()
        {
            List<MONITORINFOEX> monitors = new List<MONITORINFOEX>();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData) =>
            {
                MONITORINFOEX monitorInfo = new MONITORINFOEX();
                monitorInfo.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFOEX));
                GetMonitorInfo(hMonitor, ref monitorInfo);
                monitors.Add(monitorInfo);
                return true;
            }, IntPtr.Zero);

            return monitors;
        }

        #endregion 多显示器相关

        #region 多媒体相关

        /// <summary>
        /// 多媒体系统时间类型
        /// </summary>
        public enum TimeType
        {
            MM_ANISOTROPIC = 0,
            MM_HIMETRIC = 1,
            MM_TEXT = 2,
            MM_TWIPS = 3,
            MM_ISOTROPIC = 4,
            MM_LOMETRIC = 5,
            MM_LOENGLISH = 6,
            MM_HIENGLISH = 7
        }

        /// <summary>
        /// 音频控制命令
        /// </summary>
        [Flags]
        public enum MciCommand : uint
        {
            Close = 0x0804,
            Open = 0x0803,
            Play = 0x0806,
            Pause = 0x0809,
            Resume = 0x0855,
            Seek = 0x0807,
            Status = 0x0814,
            Stop = 0x0808,
            SetTimeFormat = 0x0810
        }

        [DllImport("winmm.dll")]
        public static extern uint mciSendString(string command, StringBuilder returnValue, int returnLength, IntPtr winHandle);

        /// <summary>
        /// 发送MCI命令
        /// </summary>
        public static string SendMciCommand(string command)
        {
            StringBuilder returnValue = new StringBuilder(256);
            uint result = mciSendString(command, returnValue, returnValue.Capacity, IntPtr.Zero);
            if (result != 0)
            {
                Debug.WriteLine($"MCI命令失败: {command}, 错误码: {result}");
                return null;
            }
            return returnValue.ToString();
        }

        #endregion 多媒体相关

        #region 环境变量和系统路径

        [DllImport("shell32.dll")]
        public static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, uint dwFlags, [Out] StringBuilder pszPath);

        [DllImport("shell32.dll")]
        public static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

        /// <summary>
        /// 获取已知文件夹路径
        /// </summary>
        public static string GetKnownFolderPath(Guid knownFolderId)
        {
            try
            {
                int result = SHGetKnownFolderPath(knownFolderId, 0, IntPtr.Zero, out IntPtr pathPtr);
                if (result >= 0)
                {
                    string path = Marshal.PtrToStringUni(pathPtr);
                    Marshal.FreeCoTaskMem(pathPtr);
                    return path;
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取已知文件夹路径失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 常用文件夹GUID
        /// </summary>
        public static class KnownFolders
        {
            public static readonly Guid Desktop = new Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641");
            public static readonly Guid Documents = new Guid("FDD39AD0-238F-46AF-ADB4-6C85480369C7");
            public static readonly Guid Downloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");
            public static readonly Guid Music = new Guid("4BD8D571-6D19-48D3-BE97-422220080E43");
            public static readonly Guid Pictures = new Guid("33E28130-4E1E-4676-835A-98395C3BC3BB");
            public static readonly Guid Videos = new Guid("18989B1D-99B5-455B-841C-AB7C74E4DDFC");
            public static readonly Guid LocalAppData = new Guid("F1B32785-6FBA-4FCF-9D55-7B8E7F157091");
            public static readonly Guid RoamingAppData = new Guid("3EB685DB-65F9-4CF6-A03A-E3EF65729F3D");
            public static readonly Guid ProgramData = new Guid("62AB5D82-FDC1-4DC3-A9DD-070D1D495D97");
            public static readonly Guid Windows = new Guid("F38BF404-1D43-42F2-9305-67DE0B28FC23");
        }

        #endregion 环境变量和系统路径

        #region 系统主题和颜色

        [DllImport("uxtheme.dll", EntryPoint = "#95")]
        public static extern uint GetImmersiveColorFromColorSetEx(uint dwImmersiveColorSet, uint dwImmersiveColorType, bool bIgnoreHighContrast, uint dwHighContrastCacheMode);

        [DllImport("uxtheme.dll", EntryPoint = "#96")]
        public static extern uint GetImmersiveColorTypeFromName(IntPtr pName);

        [DllImport("uxtheme.dll", EntryPoint = "#98")]
        public static extern int GetImmersiveUserColorSetPreference(bool forceCheckRegistry, bool skipCheckOnFail);

        [DllImport("uxtheme.dll", EntryPoint = "#100")]
        public static extern IntPtr GetImmersiveColorNamedTypeByIndex(uint dwIndex);

        [DllImport("uxtheme.dll", EntryPoint = "#135")]
        public static extern bool ShouldSystemUseDarkMode();

        [DllImport("uxtheme.dll", EntryPoint = "#138")]
        public static extern PreferredAppMode SetPreferredAppMode(PreferredAppMode appMode);

        /// <summary>
        /// 应用程序主题模式
        /// </summary>
        public enum PreferredAppMode
        {
            Default,
            AllowDark,
            ForceDark,
            ForceLight,
            Max
        }

        /// <summary>
        /// 判断系统是否使用深色模式
        /// </summary>
        public static bool IsSystemUsingDarkMode()
        {
            try
            {
                if (IsWindows10OrNewer())
                {
                    if (IsWindows11OrNewer())
                    {
                        return ShouldSystemUseDarkMode();
                    }
                    else
                    {
                        // Windows 10，通过注册表检查
                        using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                        {
                            if (key != null)
                            {
                                object value = key.GetValue("AppsUseLightTheme");
                                return value is int intValue && intValue == 0;
                            }
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"检查深色模式失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 设置应用程序主题模式
        /// </summary>
        public static bool SetAppThemeMode(PreferredAppMode mode)
        {
            try
            {
                if (IsWindows10OrNewer())
                {
                    SetPreferredAppMode(mode);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置应用主题模式失败: {ex.Message}");
                return false;
            }
        }

        #endregion 系统主题和颜色

        #region 文件关联

        /// <summary>
        /// 注册文件关联
        /// </summary>
        public static bool RegisterFileAssociation(string extension, string progId, string description, string icon, string command)
        {
            try
            {
                // 确保扩展名以点号开头
                if (!extension.StartsWith("."))
                    extension = "." + extension;

                // 注册扩展名
                using (var extKey = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(extension))
                {
                    extKey.SetValue("", progId);
                }

                // 注册ProgID
                using (var progIdKey = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey(progId))
                {
                    progIdKey.SetValue("", description);

                    // 设置图标
                    using (var iconKey = progIdKey.CreateSubKey("DefaultIcon"))
                    {
                        iconKey.SetValue("", icon);
                    }

                    // 设置命令
                    using (var shellKey = progIdKey.CreateSubKey("shell"))
                    using (var openKey = shellKey.CreateSubKey("open"))
                    using (var commandKey = openKey.CreateSubKey("command"))
                    {
                        commandKey.SetValue("", command);
                    }
                }

                // 通知系统更新
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"注册文件关联失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 取消文件关联注册
        /// </summary>
        public static bool UnregisterFileAssociation(string extension, string progId)
        {
            try
            {
                // 确保扩展名以点号开头
                if (!extension.StartsWith("."))
                    extension = "." + extension;

                // 检查并删除扩展名关联
                using (var extKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(extension, true))
                {
                    if (extKey != null)
                    {
                        string currentProgId = extKey.GetValue("") as string;
                        if (string.Equals(currentProgId, progId, StringComparison.OrdinalIgnoreCase))
                        {
                            Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(extension);
                        }
                    }
                }

                // 删除ProgID
                if (Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(progId) != null)
                {
                    Microsoft.Win32.Registry.ClassesRoot.DeleteSubKeyTree(progId);
                }

                // 通知系统更新
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"取消文件关联注册失败: {ex.Message}");
                return false;
            }
        }

        [DllImport("shell32.dll")]
        public static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        #endregion 文件关联

        #region 系统信息和指标

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
        {
            public ushort wProcessorArchitecture;
            public ushort wReserved;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public IntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public ushort wProcessorLevel;
            public ushort wProcessorRevision;
        }

        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        /// <summary>
        /// 获取系统信息
        /// </summary>
        public static SYSTEM_INFO GetSystemInfo()
        {
            SYSTEM_INFO sysInfo;
            GetSystemInfo(out sysInfo);
            return sysInfo;
        }

        #endregion 系统信息和指标

        #region 附加工具方法

        /// <summary>
        /// 转换Windows RECT为.NET Rectangle
        /// </summary>
        public static Rectangle ToRectangle(RECT rect)
        {
            return rect.ToRectangle();
        }

        /// <summary>
        /// 转换.NET Rectangle为Windows RECT
        /// </summary>
        public static RECT ToRECT(Rectangle rectangle)
        {
            return new RECT(rectangle);
        }

        #endregion 附加工具方法
    }
}