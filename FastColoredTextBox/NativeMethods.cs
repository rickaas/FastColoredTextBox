using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FastColoredTextBoxNS
{
    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        internal static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);

        [DllImport("User32.dll")]
        internal static extern bool CreateCaret(IntPtr hWnd, int hBitmap, int nWidth, int nHeight);

        [DllImport("User32.dll")]
        internal static extern bool SetCaretPos(int x, int y);

        [DllImport("User32.dll")]
        internal static extern bool DestroyCaret();

        [DllImport("User32.dll")]
        internal static extern bool ShowCaret(IntPtr hWnd);

        [DllImport("User32.dll")]
        internal static extern bool HideCaret(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetOpenClipboardWindow();

        [DllImport("user32.dll")]
        internal static extern IntPtr CloseClipboard();

        [DllImport("Imm32.dll")]
        internal static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("Imm32.dll")]
        internal static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);
    }
}
