// Author: František Holubec
// Created: 23.04.2025

using System;

namespace EDIVE.Audio
{
    public static class AudioUtils
    {
        public static void CheckMicrophonePermission(Action<bool> callback)
        {
#if UNITY_ANDROID
            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                callback?.Invoke(true);
                return;
            }

            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone, callbacks);
            callbacks.PermissionGranted += _ => callback?.Invoke(true);
            callbacks.PermissionDenied += _ => callback?.Invoke(false);
#else
            callback?.Invoke(true);
#endif
        }

    }
}
