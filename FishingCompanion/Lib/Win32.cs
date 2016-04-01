using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace FishingCompanion.Lib
{
    public class Win32
    {
        // Message code definitions
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int WK_FISHING = 0x31; // Key_1
        private const int WK_BAIT = 0x32; // Key_2


        [DllImport("User32.Dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("User32.Dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("User32.Dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.Dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        [DllImport("User32.Dll")]
        private static extern long SetCursorPos(int x, int y);

        [DllImport("User32.Dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        /// <summary>
        /// Finds and returns window handle
        /// </summary>
        /// <param name="appName"></param>
        /// <returns></returns>
        public static IntPtr GetWindowHandle(string appName)
        {
            return FindWindow(null, appName);
        }

        /// <summary>
        /// Brings given window to the front
        /// </summary>
        /// <param name="hWnd"></param>
        public static void ActivateWindow(IntPtr hWnd)
        {
            SetForegroundWindow(hWnd);
        }

        /// <summary>
        /// Returns current cursor information
        /// </summary>
        /// <returns></returns>
        public static CURSORINFO GetCursor()
        {
            CURSORINFO curInfo;
            curInfo.cbSize = Marshal.SizeOf(typeof (CURSORINFO));
            GetCursorInfo(out curInfo);

            return curInfo;
        }

        /// <summary>
        /// Returns the position information of the cursor
        /// </summary>
        /// <returns></returns>
        public static POINT GetCursorPosition()
        {
            CURSORINFO curInfo;
            curInfo.cbSize = Marshal.SizeOf(typeof (CURSORINFO));
            GetCursorInfo(out curInfo);

            return curInfo.ptScreenPos;
        }

        /// <summary>
        /// Returns current window rectangle
        /// </summary>
        /// <returns></returns>
        public static RECT GetWindowRect(IntPtr hWnd)
        {
            RECT windowRect;
            GetWindowRect(hWnd, out windowRect);

            return windowRect;
        }

        /// <summary>
        /// Resets the mouse coordinates and returns default game cursor
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static CURSORINFO GetDefaultGameCursor(IntPtr hWnd)
        {
            var windowRect = GetWindowRect(hWnd);

            MoveMouse(windowRect.Right, windowRect.Bottom);
            Thread.Sleep(100);

            CURSORINFO curInfo;
            curInfo.cbSize = Marshal.SizeOf(typeof (CURSORINFO));
            GetCursorInfo(out curInfo);

            return curInfo;
        }

        /// <summary>
        /// Moves mouse to given position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public static void MoveMouse(int x, int y)
        {
            SetCursorPos(x, y);
        }

        /// <summary>
        /// Sends left mouse button click message
        /// </summary>
        /// <param name="hWnd"></param>
        public static void SendMouseClick(IntPtr hWnd)
        {
            SendMessage(hWnd, WM_RBUTTONDOWN, 0, 0);
            Thread.Sleep(200);
            SendMessage(hWnd, WM_RBUTTONUP, 0, 0);
        }

        /// <summary>
        /// Sends key press (Key_1) message
        /// </summary>
        /// <param name="hWnd"></param>
        public static void CastFishing(IntPtr hWnd)
        {
            SendMessage(hWnd, WM_KEYDOWN, WK_FISHING, 0);
            Thread.Sleep(200);
            SendMessage(hWnd, WM_KEYUP, WK_FISHING, 0);
        }

        /// <summary>
        /// Sends key press (Key_2) message
        /// </summary>
        /// <param name="hWnd"></param>
        public static void CastBait(IntPtr hWnd)
        {
            SendMessage(hWnd, WM_KEYDOWN, WK_BAIT, 0);
            Thread.Sleep(200);
            SendMessage(hWnd, WM_KEYUP, WK_BAIT, 0);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}