// Author: František Holubec
// Created: 07.04.2026

#if SR_DEBUGGER
using System;
using EDIVE.Core;
using EDIVE.NativeUtils;
using UnityEngine;

namespace EDIVE.Utils.BugReporting
{
    public static class SRDebuggerExtensions
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            // SRDebug spins up a KeyboardShortcutListenerService that NREs without an input device on headless builds.
            if (PlatformUtils.IsHeadless())
                return;

            SRDebug.Instance.AddOptionContainer(new DebugOptions());
        }
        
        public class DebugOptions
        {
            public void ThrowDebugException()
            {
                AppCore.Instance.InvokeOnEndOfFrame(() => throw new Exception("Debug Exception"));
            }
            
            public void SendBugReport()
            {
                if (!AppCore.Services.TryGet<BugReportingManager>(out var bugReportingManager))
                {
                    Debug.LogError("BugReportingManager not found");
                    return;
                }
                bugReportingManager.TrySendReport();
            }
        }
    }
}
#endif
