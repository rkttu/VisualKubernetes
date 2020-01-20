using System;
using System.Runtime.InteropServices;

namespace VisualKubernetes
{
    internal static class NativeMethods
    {
        public const int TCM_SETMINTABWIDTH = 0x1300 + 49;

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
    }
}
