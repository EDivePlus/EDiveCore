// Author: František Holubec
// Created: 22.03.2025

using Mirror;
using UnityEngine;
using UnityEngine.XR;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.MirrorNetworking.Utils
{
    public static class NetworkUtils
    {
#if UNITY_EDITOR
        private static GlobalPersistentContext<NetworkRuntimeMode> EditorRuntimeModeContext =>
            PersistentContext.Get(Application.dataPath, nameof(MirrorNetworking), nameof(NetworkRuntimeMode), NetworkRuntimeMode.Client);

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

        public static bool TryGetTransport<T>(this NetworkManager manager, out T result) where T : Transport
        {
            var rootTransport = manager.transport;
            return rootTransport.TryGetTransport(out result);
        }

        private static bool TryGetTransport<T>(this Transport parentTransport, out T result) where T : Transport
        {
            if (parentTransport == null)
            {
                result = null;
                return false;
            }

            if (parentTransport is T mainTransport)
            {
                result = mainTransport;
                return true;
            }

            if (parentTransport is MiddlewareTransport middlewareTransport && middlewareTransport.inner.TryGetTransport(out result))
                return true;

            if (parentTransport is MultiplexTransport multiplexTransport)
            {
                foreach (var childMultiplexTransport in multiplexTransport.transports)
                {
                    if (childMultiplexTransport.TryGetTransport(out result))
                        return true;
                }
            }

            result = null;
            return false;
        }
    }
}
