using System;
using EDIVE.AppLoading;
using UnityEngine;
using UnityEngine.Events;

namespace EDIVE.VoiceChat
{
    public abstract class AVoiceChatManager : ALoadableServiceBehaviour<AVoiceChatManager>
    {
        [Serializable]
        public class UnityEventMute : UnityEvent<string, bool> { }

        private UnityEventMute m_onLocalMute = new UnityEventMute();

        public UnityEventMute OnLocalMute => m_onLocalMute;

        public bool MuteMicOnConnect { get; set; }
        public abstract bool EnableSpatialAudio { get; set; }
        public abstract int MicFrameDurationMS { get; set; }
        public abstract bool AllowMic { get; set; }
        public abstract int CurrentMicIndex { get; set; }
        
        public void OnApplicationQuit()
        {
            StopVoiceChat();
        }


        public virtual void StartVoiceChat(string id)
        {

        }

        public virtual void StopVoiceChat()
        {

        }

        public virtual bool IsMicMuted()
        {
            return true;
        }

        public virtual void SetMicMuted(bool value)
        {

        }

        public virtual bool IsSpeaking(string iD)
        {
            return false;
        }

        public virtual void ToggleMuteOtherUser(string iD)
        {

        }

        public virtual void SetWorldPosition(Vector3 position, Vector3 forward, Vector3 up)
        {

        }

        // TODO normalize
        // Between -50 and 50 on a log scale
        public virtual void SetMicVolume(int value)
        {

        }

        // TODO normalize
        // Between -50 and 50 on a log scale
        public virtual void SetVoiceChatVolume(int value)
        {

        }
    }
}