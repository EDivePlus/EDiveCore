// Author: František Holubec
// Created: 23.04.2025

using Cysharp.Threading.Tasks;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace EDIVE.VoiceChat
{
    public static class VoiceChatUtils
    {
        public static async UniTask<bool> AwaitVoicePermission()
        {
#if UNITY_ANDROID
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
                return true;

            var callbacks = new PermissionCallbacks();
            Permission.RequestUserPermission(Permission.Microphone, callbacks);
            var completionSource = new UniTaskCompletionSource<bool>();
            callbacks.PermissionGranted += _ => completionSource.TrySetResult(true);
            callbacks.PermissionDenied += _ => completionSource.TrySetResult(false);

            return await completionSource.Task;
#else
            return true;
#endif

        }
    }
}
