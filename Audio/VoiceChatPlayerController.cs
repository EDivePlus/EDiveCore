// Author: František Holubec
// Created: 23.04.2025

using EDIVE.External.Promises;
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
        
        public EnhancedStreamedAudioSourceOutput AudioOutput { get; private set; }
        public Promise<EnhancedStreamedAudioSourceOutput> AudioOutputPromise { get; } = new();
        
        public void InitializePeerAudioOutput(EnhancedStreamedAudioSourceOutput audioOutput)
        {
            AudioOutput = audioOutput;
            AudioOutputPromise.Dispatch(AudioOutput);
            
            var outputTransform = audioOutput.transform;
            outputTransform.SetParent(_PeerRoot); 
            outputTransform.localPosition = Vector3.zero;
        }
    }
}
