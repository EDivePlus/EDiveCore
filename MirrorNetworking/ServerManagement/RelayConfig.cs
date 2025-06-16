// Author: František Holubec
// Created: 15.06.2025

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
        private string _NodePort;

        [JsonProperty]
        [SerializeField]
        private string _EndpointPort;

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
        private string _LoadBalancerPort;

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

    }
}
