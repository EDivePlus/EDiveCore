// Author: František Holubec
// Created: 13.03.2026

#if UNITY_EDITOR
using EDIVE.External.DomainReloadHelper;
#if XR_HANDS
using UnityEngine.XR.Hands.Samples.Gestures.DebugTools;
#endif


namespace EDIVE.XRTools.Editor
{
    public static class XRToolkitEditorUtils
    {
        [ExecuteOnReload]
        private static void ClearToolkitDomain()
        {
#if XR_HANDS
            // Clear subsystems cache
            DomainReloadHandler.ClearFieldToNew(typeof(XRAllFingerShapesDebugUI)
                .GetField("s_SubsystemsReuse", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static));
            
            // Clear capture playback
            var playbackType = System.Type.GetType("UnityEditor.XR.Hands.Capture.XRHandCapturePlayback, Unity.XR.Hands.Editor");
            if (playbackType != null)
            {
                var getInstance = playbackType.GetMethod("GetInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                var instance = getInstance?.Invoke(null, null);
                if (instance != null)
                {
                    var onDestroy = playbackType.GetMethod("OnDestroy", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    onDestroy?.Invoke(instance, null);
                }
            }
#endif
        }
    }
}
#endif
