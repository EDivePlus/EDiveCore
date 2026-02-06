// Author: František Holubec
// Created: 23.04.2025

using Adrenak.UniVoice.Outputs;
using EDIVE.Networking.Players;
using UnityEngine;

namespace EDIVE.Audio
{
    public class VoiceChatPlayerController : MonoBehaviour
    {
        [SerializeField]
        private NetworkPlayerController _PlayerController;
        
        [SerializeField]
        private Transform _PeerRoot;
        
        
        public void InitializePeerAudioOutput(StreamedAudioSourceOutput audioOutput)
        {
            var outputTransform = audioOutput.transform;
            outputTransform.SetParent(_PeerRoot); 
            outputTransform.localPosition = Vector3.zero;
        }
    }
}
