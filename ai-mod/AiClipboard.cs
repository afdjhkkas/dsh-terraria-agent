using System;
using System.Runtime.InteropServices;

namespace DshAiMini
{
    /// <summary>
    /// 读系统剪贴板。
    ///
    /// 为什么不用现成的：tModLoader 和 Terraria 都没有公开的剪贴板 API
    /// （ReLogic.OS.Platform.Clipboard 在 ReLogic 里，但没导出到这个程序集），
    /// 而 .NET 8 的 WinForms/WPF 剪贴板又不在 mod 的引用里。
    /// 直接 P/Invoke 反而最稳：只有三个函数，失败时返回空串，不会炸。
    /// </summary>
    internal static class AiClipboard
    {
        private const uint CF_UNICODETEXT = 13;

        [DllImport("user32.dll", SetLastError = true)] private static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool CloseClipboard();
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr GetClipboardData(uint uFormat);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool IsClipboardFormatAvailable(uint format);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GlobalUnlock(IntPtr hMem);

        public static string GetText()
        {
            try
            {
                if (!IsClipboardFormatAvailable(CF_UNICODETEXT)) return "";
                if (!OpenClipboard(IntPtr.Zero)) return "";   // 别的程序占着剪贴板时会失败，正常
                try
                {
                    IntPtr h = GetClipboardData(CF_UNICODETEXT);
                    if (h == IntPtr.Zero) return "";
                    IntPtr p = GlobalLock(h);
                    if (p == IntPtr.Zero) return "";
                    try { return Marshal.PtrToStringUni(p) ?? ""; }
                    finally { GlobalUnlock(h); }
                }
                finally { CloseClipboard(); }
            }
            catch { return ""; }
        }
    }
}
