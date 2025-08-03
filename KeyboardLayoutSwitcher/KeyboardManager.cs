using System;
using System.Runtime.InteropServices;

namespace KeyboardLayoutSwitcher
{
    public static class KeyboardManager
    {
        [DllImport("user32.dll")]
        private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool ActivateKeyboardLayout(IntPtr hkl, uint Flags);

        public static void SwitchLayout(string layoutHex)
        {
            if (string.IsNullOrWhiteSpace(layoutHex) || layoutHex.Length != 8)
                return;

            IntPtr hkl = LoadKeyboardLayout(layoutHex, 1); // 1 = KLF_ACTIVATE

            // Active la disposition pour la fenêtre au premier plan
            IntPtr hwnd = GetForegroundWindow();
            uint threadId = GetWindowThreadProcessId(hwnd, out _);
            ActivateKeyboardLayout(hkl, 0);
        }
    }
}