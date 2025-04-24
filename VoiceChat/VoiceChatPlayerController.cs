// Author: František Holubec
// Created: 23.04.2025

using EDIVE.MirrorNetworking.Players;
using UnityEngine;

namespace EDIVE.VoiceChat
{
    public class VoiceChatPlayerController : MonoBehaviour
    {
        [SerializeField]
        private NetworkPlayerController _PlayerController;

        [SerializeField]
        private Transform _PeerRoot;

        public int ConnectionID => _PlayerController.ConnectionID;
        public Transform PeerRoot => _PeerRoot;
    }
}
