// Author: František Holubec
// Created: 22.03.2025

using Sirenix.OdinInspector.Editor;
using UnityEngine;
using UnityEngine.XR;

namespace EDIVE.Networking.Utils
{
    public static class NetworkUtils
    {
#if UNITY_EDITOR
        private static GlobalPersistentContext<NetworkRuntimeMode> EditorRuntimeModeContext =>
            PersistentContext.Get(Application.dataPath, nameof(Networking), nameof(NetworkRuntimeMode), NetworkRuntimeMode.Client);

        public static NetworkRuntimeMode EditorRuntimeMode
        {
            get => EditorRuntimeModeContext.Value;
            set => EditorRuntimeModeContext.Value = value;
        }
#endif

        public static NetworkRuntimeMode RuntimeMode =>
#if UNITY_EDITOR
            EditorRuntimeMode;
#else
            Mirror.Utils.IsHeadless() ? NetworkRuntimeMode.Server : NetworkRuntimeMode.Client;
#endif

        public static ClientPlatformType ClientPlatformType => XRSettings.enabled ? ClientPlatformType.Headset : ClientPlatformType.Desktop;
    }
}
