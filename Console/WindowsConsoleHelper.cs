// Author: František Holubec
// Created: 20.04.2026

#if UNITY_STANDALONE_WIN
using System;
using System.Runtime.InteropServices;

namespace EDIVE.Console
{
    public static class WindowsConsoleHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr handle, out uint mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr handle, uint mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleOutputCP(uint codePageId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCP(uint codePageId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetConsoleTitleW(string lpConsoleTitle);

        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint CP_UTF8 = 65001;

        public static void Configure()
        {
            try
            {
                SetConsoleOutputCP(CP_UTF8);
                SetConsoleCP(CP_UTF8);

                var handle = GetStdHandle(STD_OUTPUT_HANDLE);
                if (GetConsoleMode(handle, out var mode))
                    SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
            }
            catch { }
        }

        public static void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return;
            try { SetConsoleTitleW(title); }
            catch { }
        }
    }
}
#endif