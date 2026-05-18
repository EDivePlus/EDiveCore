// Author: František Holubec
// Created: 18.05.2026

using EDIVE.NativeUtils;
using SRDebugger;
using UnityEngine;

namespace EDIVE.External.SRDebugger
{
    public static class SRDebugerInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void OnLoad()
        {
            if (!PlatformUtils.IsHeadless() && Settings.Instance.IsEnabled)
            {
                SRDebug.Init();
            }
        }
    }
}
