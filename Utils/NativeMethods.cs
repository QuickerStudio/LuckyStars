using System;
using System.Runtime.InteropServices;

namespace LuckyStars.Utils
{
    /// <summary>
    /// 提供对Windows API的统一访问
    /// 使用LibraryImport替代传统的DllImport以提高性能和安全性
    /// </summary>
    public static partial class NativeMethods
    {
        #region 常量定义

        // 窗口样式常量
        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_CHILD = 0x40000000;
        public const int HWND_BOTTOM = 1;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint LWA_ALPHA = 0x00000002;

        // 自定义消息ID，用于发送鼠标坐标（WM_USER+100）
        public const int WM_USER_MOUSE = 0x0400 + 100;

        #endregion

        #region 结构体定义

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        #endregion

        #region 窗口管理API

        [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        public static partial IntPtr FindWindow(
            [MarshalAs(UnmanagedType.LPWStr)] string? lpClassName,
            [MarshalAs(UnmanagedType.LPWStr)] string? lpWindowName);

        [LibraryImport("user32.dll", EntryPoint = "FindWindowExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        public static partial IntPtr FindWindowEx(
            IntPtr parentHandle,
            IntPtr childAfter,
            [MarshalAs(UnmanagedType.LPWStr)] string? className,
            [MarshalAs(UnmanagedType.LPWStr)] string? windowTitle);

        [LibraryImport("user32.dll", EntryPoint = "SetParent", SetLastError = true)]
        public static partial IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
        public static partial int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
        public static partial int GetWindowLong(IntPtr hWnd, int nIndex);

        [LibraryImport("user32.dll", EntryPoint = "SetWindowPos", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        [LibraryImport("user32.dll", EntryPoint = "GetDesktopWindow", SetLastError = true)]
        public static partial IntPtr GetDesktopWindow();

        [LibraryImport("user32.dll", EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
        public static partial uint SendMessageTimeout(
            IntPtr hWnd,
            uint Msg,
            IntPtr wParam,
            IntPtr lParam,
            uint fuFlags,
            uint uTimeout,
            out IntPtr lpdwResult);

        [LibraryImport("user32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
        public static partial IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [LibraryImport("user32.dll", EntryPoint = "GetWindowRect", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [LibraryImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        #endregion

        #region 鼠标和键盘API

        [LibraryImport("user32.dll", EntryPoint = "GetCursorPos", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool GetCursorPos(out POINT lpPoint);

        [LibraryImport("user32.dll", EntryPoint = "GetKeyState", SetLastError = true)]
        public static partial short GetKeyState(int nVirtKey);

        #endregion

        #region 系统API

        [LibraryImport("shell32.dll", EntryPoint = "SHAppBarMessage", SetLastError = true)]
        public static partial IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        #endregion

        #region 错误处理

        /// <summary>
        /// 获取最后一个Win32错误的详细信息
        /// </summary>
        /// <returns>错误信息</returns>
        public static string GetLastWin32Error()
        {
            int errorCode = Marshal.GetLastWin32Error();
            return new System.ComponentModel.Win32Exception(errorCode).Message;
        }

        #endregion
    }
}
