using System;
using System.Runtime.InteropServices;

namespace EmlPreviewHandler.Interop
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        public static extern IntPtr GetFocus();
    }
}
