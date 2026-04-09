// Author: František Holubec
// Created: 22.03.2025

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.Networking.Utils
{
    public static class NetworkUtils
    {
#if UNITY_EDITOR
        private static readonly EditorPrefInt EDITOR_RUNTIME_MODE_PREF = new("NetworkRuntimeMode", (int) NetworkRuntimeMode.Offline);

        public static NetworkRuntimeMode EditorRuntimeMode
        {
            get => (NetworkRuntimeMode) EDITOR_RUNTIME_MODE_PREF.Value;
            set => EDITOR_RUNTIME_MODE_PREF.Value = (int) value;
        }
#endif

        public static bool IsHeadless() =>
#if UNITY_SERVER
            true;
#else
            SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
#endif
        
        public static NetworkRuntimeMode RuntimeMode =>
#if UNITY_EDITOR
            EditorRuntimeMode;
#else
            IsHeadless() ? NetworkRuntimeMode.Server : NetworkRuntimeMode.Offline;
#endif

        public static ClientPlatformType ClientPlatformType => XRSettings.enabled ? ClientPlatformType.Headset : ClientPlatformType.Desktop;
    }
}
