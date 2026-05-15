// Author: František Holubec
// Created: 15.05.2026

#if UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
namespace EDIVE.Console
{
    public static class PosixConsoleHelper
    {
        public static void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return;
            try
            {
                System.Console.Write($"\u001b]0;{title}\u0007");
                System.Console.Out.Flush();
            }
            catch { }
        }
    }
}
#endif
