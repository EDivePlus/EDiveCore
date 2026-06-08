// Author: František Holubec
// Created: 23.04.2025

using EDIVE.Audio.Playback;
using UnityEngine;

namespace EDIVE.Audio
{
    public class VoiceChatPlayerController : MonoBehaviour
    {
        [SerializeField]
        private Transform _PeerRoot;
        
        private StreamedAudioSourceOutput _audioOutput;
        
        public void InitializePeerAudioOutput(StreamedAudioSourceOutput audioOutput)
        {
            _audioOutput = audioOutput;
        }

        private void Update()
        {
            if (_audioOutput != null)
                _audioOutput.transform.position = _PeerRoot.position;
        }
    }
}
