// Author: František Holubec
// Created: 21.03.2025

using System;
using UnityEditor;
using UnityEditor.Build;

namespace EDIVE.BuildTool.Utils
{
    [Flags]
    public enum PlatformType
    {
        WindowsClient = 1 << 0,
        WindowsServer = 1 << 1,
        LinuxClient = 1 << 2,
        LinuxServer = 1 << 3,
        MacOSClient = 1 << 4,
        MacOSServer = 1 << 5,
        Android = 1 << 6,
        IOS = 1 << 7,

        AnyWindows = WindowsClient | WindowsServer,
        AnyLinux = LinuxClient | LinuxServer,
        AnyMacOS = MacOSClient | MacOSServer,

        AnyClient = WindowsClient | LinuxClient | MacOSClient | Android | IOS,
        AnyServer = WindowsServer | LinuxServer | MacOSServer,

        All = ~(-1 << 8)
    }

    public static class PlatformTypeExtensions
    {
        public static bool ContainsTarget(this PlatformType platformType, NamedBuildTarget namedBuildTarget, BuildTarget target)
        {
            if (namedBuildTarget == NamedBuildTarget.Standalone) return target switch
            {
                BuildTarget.StandaloneWindows64 => platformType.HasFlag(PlatformType.WindowsClient),
                BuildTarget.StandaloneLinux64 => platformType.HasFlag(PlatformType.LinuxClient),
                BuildTarget.StandaloneOSX => platformType.HasFlag(PlatformType.MacOSClient),
                _ => false
            };
            if (namedBuildTarget == NamedBuildTarget.Server) return target switch
            {
                BuildTarget.StandaloneWindows64 => platformType.HasFlag(PlatformType.WindowsServer),
                BuildTarget.StandaloneLinux64 => platformType.HasFlag(PlatformType.LinuxServer),
                BuildTarget.StandaloneOSX => platformType.HasFlag(PlatformType.MacOSServer),
                _ => false
            };
            if (namedBuildTarget == NamedBuildTarget.Android) return platformType.HasFlag(PlatformType.Android);
            if (namedBuildTarget == NamedBuildTarget.iOS) return platformType.HasFlag(PlatformType.IOS);
            return false;
        }
    }
}
