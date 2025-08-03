using System;
using System.Runtime.InteropServices;

namespace KeyboardLayoutSwitcher
{
    public static class KeyboardManager
    {
        [DllImport("user32.dll")]
        private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);

        public static void SwitchLayout(string layoutHex)
        {
            // Exemple : "00000409" pour QWERTY US, "0000040C" pour AZERTY FR
            LoadKeyboardLayout(layoutHex, 1);
        }
    }
}