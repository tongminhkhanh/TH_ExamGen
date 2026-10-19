using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TH_ExamGen.UI.Helpers
{
    /// <summary>
    /// Flashes the application's taskbar button when a long-running AI task finishes,
    /// so the teacher notices the result while working in another window.
    /// Uses FLASHW_TIMERNOFG so the flashing stops automatically once the window
    /// regains focus, and does nothing when the window is already in the foreground.
    /// </summary>
    public static class TaskbarFlash
    {
        private const uint FLASHW_ALL = 0x00000003;
        private const uint FLASHW_TIMERNOFG = 0x0000000C;

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        public static void Flash(Window? window)
        {
            if (window == null) return;
            if (window.IsActive) return;

            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            var info = new FLASHWINFO
            {
                cbSize = (uint)Marshal.SizeOf<FLASHWINFO>(),
                hwnd = hwnd,
                dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG,
                uCount = uint.MaxValue,
                dwTimeout = 0
            };
            FlashWindowEx(ref info);
        }
    }
}
