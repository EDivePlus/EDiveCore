// Author: František Holubec
// Created: 15.10.2026

#if DEBUG_MENU
using EDIVE.Core;
using EDIVE.Debugging;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Utils.BugReporting
{
    public static class BugReportingDebugMenu
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            if (PlatformUtils.IsHeadless())
                return;

            DebugMenu.Panel("App").Icon("apps");
            DebugMenu.Section("App", "Bug report").Button("Send bug report", SendReport);
        }

        private static void SendReport()
        {
            if (!AppCore.Services.TryGet<BugReportingManager>(out var manager))
            {
                DebugMenu.Toast("Bug reporting is not available", DebugToastLevel.Warning);
                return;
            }

            manager.TrySendReport();
            DebugMenu.Toast("Bug report sent", DebugToastLevel.Success);
        }
    }
}
#endif
