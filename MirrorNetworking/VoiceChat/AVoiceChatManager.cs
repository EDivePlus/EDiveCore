using System;
using Cysharp.Threading.Tasks;
using EDIVE.Core.Services;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace EDIVE.VoiceChat
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

        public async UniTask<bool> AwaitVoicePermission()
        {
#if UNITY_ANDROID// && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
                return true;

            var callbacks = new PermissionCallbacks();
            Permission.RequestUserPermission(Permission.Microphone, callbacks);
            var completionSource = new UniTaskCompletionSource<bool>();
            callbacks.PermissionGranted += _ => completionSource.TrySetResult(true);
            callbacks.PermissionDenied += _ => completionSource.TrySetResult(false);
            callbacks.PermissionDeniedAndDontAskAgain += _ => completionSource.TrySetResult(false);

            return await completionSource.Task;
#endif
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