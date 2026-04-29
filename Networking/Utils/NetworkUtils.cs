// Author: František Holubec
// Created: 22.03.2025

using System.Linq;
using System.Net;
using System.Net.Sockets;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

#if UNITY_EDITOR
using System.IO;
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.Networking.Utils
{
    public static class NetworkUtils
    {
#if UNITY_EDITOR
        private static readonly EditorPrefInt EDITOR_RUNTIME_MODE_PREF = new($"NetworkRuntimeMode-{Directory.GetParent(Application.dataPath)?.Name}", (int) NetworkRuntimeMode.Offline);

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

        // Best-effort local IPv4 lookup for displaying the host's LAN address. Returns "0.0.0.0" if no
        // routable adapter is found (DNS/host resolution failure or all-loopback).
        public static string GetLocalIPv4()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ipv4 = host.AddressList.LastOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
                return ipv4 != null ? ipv4.ToString() : "0.0.0.0";
            }
            catch
            {
                return "0.0.0.0";
            }
        }

        // Asks the OS for a free UDP port
        public static ushort FindFreeUdpPort()
        {
            using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
            return (ushort) ((IPEndPoint) udp.Client.LocalEndPoint).Port;
        }

        // Finds free UDP port in range [min, max] (inclusive) for a free UDP port.
        // Falls back to OS-assigned
        public static ushort FindFreeUdpPort(ushort min, ushort max)
        {
            if (min == 0 || max == 0 || min > max)
                return FindFreeUdpPort();

            for (var port = (int) min; port <= max; port++)
            {
                try
                {
                    using var udp = new UdpClient(new IPEndPoint(IPAddress.Any, port));
                    return (ushort) port;
                }
                catch (SocketException)
                {
                }
            }

            Debug.LogWarning($"[NetworkUtils] No free UDP port in [{min},{max}], falling back to OS-assigned.");
            return FindFreeUdpPort();
        }

        // Probes a UDP endpoint to decide whether direct connect is worth attempting. Tugboat won't
        // answer arbitrary packets, so we rely on ICMP port-unreachable: on Windows that surfaces as
        // SocketError.ConnectionReset on the next socket op (Linux: ConnectionRefused). UdpClient.Connect
        // pins the remote endpoint so those ICMP errors are routed back to us as exceptions.
        // Returns false only when we have a definite "port closed" signal; timeout / filtered → true,
        // letting the actual connection attempt make the final call.
        public static async UniTask<bool> IsServerReachable(string ip, int port, int timeoutMs = 1000)
        {
            UdpClient udp = null;
            try
            {
                udp = new UdpClient();
                udp.Connect(ip, port);
                await udp.SendAsync(new byte[] { 0 }, 1);

                var (probeWon, probeResult) = await UniTask.WhenAny(ReceiveProbe(udp), UniTask.Delay(timeoutMs));
                // Got a definitive answer (reply or ICMP unreachable) → use it. Timeout → assume reachable.
                return !probeWon || probeResult;
            }
            catch
            {
                return false;
            }
            finally
            {
                udp?.Close();
            }
        }

        private static async UniTask<bool> ReceiveProbe(UdpClient udp)
        {
            try
            {
                await udp.ReceiveAsync();
                return true;
            }
            catch
            {
                return true;
            }
        }
    }
}
