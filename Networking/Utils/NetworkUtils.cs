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
        private static GlobalPersistentContext<NetworkRuntimeMode> EditorRuntimeModeContext =>
            PersistentContext.Get(Application.dataPath, nameof(Networking), nameof(NetworkRuntimeMode), NetworkRuntimeMode.Offline);

        public static NetworkRuntimeMode EditorRuntimeMode
        {
            get => EditorRuntimeModeContext.Value;
            set => EditorRuntimeModeContext.Value = value;
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
            IsHeadless() ? NetworkRuntimeMode.Server : NetworkRuntimeMode.Client;
#endif

        public static ClientPlatformType ClientPlatformType => XRSettings.enabled ? ClientPlatformType.Headset : ClientPlatformType.Desktop;
    }
}
