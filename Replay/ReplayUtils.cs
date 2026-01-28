// Author: František Holubec
// Created: 06.07.2025

using System.IO;
using EDIVE.AssetTranslation;
using EDIVE.NativeUtils;
using EDIVE.Replay.Frames;
using EDIVE.Utils.Cysharp;
using UnityEngine;

namespace EDIVE.Replay
{
    public static class ReplayUtils
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        private static void Initialize()
        {
            MemoryPackUtility.RegisterDynamicUnionFormatter<AFrameSequence>();
            AssetTranslationMemoryPackUtils.RegisterTranslator<ReplayAgentDefinition>();
        }
        
        public static string RecordingsFolderPath => PathUtility.GetRootAppDataPath("ReplayRecordings");
        
        public static string GetRecordingSaveFileName(string id, string extension = ".dat")
        {
            var folder = RecordingsFolderPath;
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            
            return Path.Combine(folder, $"{id}{extension}");
        }
    }
}
