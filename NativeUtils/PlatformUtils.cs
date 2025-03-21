using System;
using UnityEditor;
using UnityEngine;

namespace EDIVE.NativeUtils
{
    public static class PlatformUtils
    {
        public static RuntimePlatform GetCurrentBuildTarget()
        {
#if UNITY_EDITOR
            return EditorUserBuildSettings.activeBuildTarget.ToRuntimePlatform();
#else
            return Application.platform;
#endif
        }

#if UNITY_EDITOR
        private static RuntimePlatform ToRuntimePlatform(this BuildTarget buildTarget)
        {
            return buildTarget switch
            {
                BuildTarget.StandaloneOSX => RuntimePlatform.OSXPlayer,
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => RuntimePlatform.WindowsPlayer,
                BuildTarget.StandaloneLinux64 => RuntimePlatform.LinuxPlayer,
                BuildTarget.iOS => RuntimePlatform.IPhonePlayer,
                BuildTarget.Android => RuntimePlatform.Android,
                BuildTarget.WebGL => RuntimePlatform.WebGLPlayer,
                BuildTarget.WSAPlayer => RuntimePlatform.WSAPlayerX86,
                BuildTarget.PS4 => RuntimePlatform.PS4,
                BuildTarget.PS5 => RuntimePlatform.PS5,
                BuildTarget.XboxOne => RuntimePlatform.XboxOne,
                BuildTarget.GameCoreXboxOne => RuntimePlatform.GameCoreXboxOne,
                BuildTarget.tvOS => RuntimePlatform.tvOS,
                BuildTarget.Switch => RuntimePlatform.Switch,
                BuildTarget.EmbeddedLinux => RuntimePlatform.EmbeddedLinuxX64,
                BuildTarget.QNX => RuntimePlatform.QNXX64,
                BuildTarget.VisionOS => RuntimePlatform.VisionOS,
                _ => throw new Exception("Unknown build target: " + buildTarget)
            };
        }
#endif
    }
}
