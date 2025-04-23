using System;
using EDIVE.Core.Services;
using UnityEngine;
using UnityEngine.Events;

namespace EDIVE.MirrorNetworking.VoiceChat
{
    public abstract class AVoiceChatManager : AServiceBehaviour<AVoiceChatManager>
    {
        [Serializable]
        public class UnityEventMute : UnityEvent<string, bool> { }

        private UnityEventMute m_onLocalMute = new UnityEventMute();

        public UnityEventMute OnLocalMute => m_onLocalMute;

        public bool MuteMicOnConnect { get; set; }

        public void OnApplicationQuit()
        {
            StopVoiceChat();
        }

        public abstract bool IsMicMuted();

        public abstract void StartVoiceChat(string id);
        public abstract void StopVoiceChat();
        public abstract void SetMicMuted(bool value);

        public abstract bool IsSpeaking(string iD);
        public abstract void ToggleMuteOtherUser(string iD);
        public abstract void SetWorldPosition(Vector3 position, Vector3 forward, Vector3 up);

        // TODO normalize between -50 and 50 on a log scale
        public abstract void SetMicVolume(int value);

        // TODO normalize between -50 and 50 on a log scale
        public abstract void SetVoiceChatVolume(int value);
    }
}