// Author: František Holubec
// Created: 23.04.2025

using Mirror;
using UnityEngine;

namespace EDIVE.MirrorNetworking.VoiceChat
{
    //TODO merge with PlayerController once its moved to submodule
    public class VoiceChatAvatar : MonoBehaviour
    {
        [SerializeField]
        private NetworkIdentity _NetworkIdentity;

        [SerializeField]
        private Transform _PeerRoot;

        public NetworkIdentity NetworkIdentity => _NetworkIdentity;
        public Transform PeerRoot => _PeerRoot;
    }
}
