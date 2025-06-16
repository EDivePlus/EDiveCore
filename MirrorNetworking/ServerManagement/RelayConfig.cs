// Author: František Holubec
// Created: 15.06.2025

using LightReflectiveMirror;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.MirrorNetworking.ServerManagement
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RelayConfig : ScriptableObject
    {
        [JsonProperty]
        [SerializeField]
        private string _NodeIP;

        [JsonProperty]
        [SerializeField]
        private ushort _NodePort;

        [JsonProperty]
        [SerializeField]
        private ushort _EndpointPort;

        [JsonProperty]
        [SerializeField]
        private string _AuthKey;

        [JsonProperty]
        [SerializeField]
        [Range(0.1f, 5f)]
        private float _HeartbeatInterval = 3f;

        [JsonProperty]
        [SerializeField]
        private bool _UseNATPunch;

        [JsonProperty]
        [SerializeField]
        private bool _UseLoadBalancer;

        [JsonProperty]
        [SerializeField]
        [EnableIf(nameof(_UseLoadBalancer))]
        private string _LoadBalancerIP;

        [JsonProperty]
        [SerializeField]
        [EnableIf(nameof(_UseLoadBalancer))]
        private ushort _LoadBalancerPort;

        [JsonProperty]
        [SerializeField]
        private int _AppID = 1;

        [JsonProperty]
        [SerializeField]
        private string _ServerName;

        [JsonProperty]
        [SerializeField]
        private int _MaxPlayers = 10;

        [JsonProperty]
        [SerializeField]
        private bool _IsPublic = true;

        public string NodeIP => _NodeIP;
        public ushort NodePort => _NodePort;
        public ushort EndpointPort => _EndpointPort;
        public string AuthKey => _AuthKey;
        public float HeartbeatInterval => _HeartbeatInterval;
        public bool UseNATPunch => _UseNATPunch;
        public bool UseLoadBalancer => _UseLoadBalancer;
        public string LoadBalancerIP => _LoadBalancerIP;
        public ushort LoadBalancerPort => _LoadBalancerPort;
        public int AppID => _AppID;
        public string ServerName => _ServerName;
        public int MaxPlayers => _MaxPlayers;
        public bool IsPublic => _IsPublic;

        public void ApplyTo(LightReflectiveMirrorTransport transport)
        {
            transport.serverIP = NodeIP;
            transport.serverPort = NodePort;
            transport.endpointServerPort = EndpointPort;
            transport.authenticationKey = AuthKey;
            transport.heartBeatInterval = HeartbeatInterval;
            transport.useNATPunch = UseNATPunch;
            transport.useLoadBalancer = UseLoadBalancer;
            transport.loadBalancerAddress = LoadBalancerIP;
            transport.loadBalancerPort = _LoadBalancerPort;
            transport.appId = AppID;
            transport.serverName = ServerName;
            transport.maxServerPlayers = MaxPlayers;
            transport.isPublicServer = IsPublic;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(transport);
#endif
        }

        public void CopyFrom(LightReflectiveMirrorTransport transport)
        {
            _NodeIP = transport.serverIP;
            _NodePort = transport.serverPort;
            _EndpointPort = transport.endpointServerPort;
            _AuthKey = transport.authenticationKey;
            _HeartbeatInterval = transport.heartBeatInterval;
            _UseNATPunch = transport.useNATPunch;
            _UseLoadBalancer = transport.useLoadBalancer;
            _LoadBalancerIP = transport.loadBalancerAddress;
            _LoadBalancerPort = transport.loadBalancerPort;
            _AppID = transport.appId;
            _ServerName = transport.serverName;
            _MaxPlayers = transport.maxServerPlayers;
            _IsPublic = transport.isPublicServer;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
