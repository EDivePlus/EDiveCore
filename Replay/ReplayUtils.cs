// Author: František Holubec
// Created: 06.07.2025

using System.IO;
using Cysharp.Threading.Tasks;
using EDIVE.AssetTranslation;
using EDIVE.NativeUtils;
using EDIVE.Replay.Agents;
using EDIVE.Replay.Components;
using EDIVE.Utils.Cysharp;
using MemoryPack;
using MemoryPack.Compression;
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
            MemoryPackUtility.RegisterDynamicUnionFormatter<AReplayAgentComponentData>();
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
        
        public static async UniTask<byte[]> SerializeAsync<T>(T record)
        {
            await UniTask.SwitchToThreadPool();
            using var compressor = new BrotliCompressor();
            MemoryPackSerializer.Serialize(compressor, record);
            return compressor.ToArray();
        }
        
        public static  async UniTask<T> DeserializeAsync<T>(byte[] data)
        {
            await UniTask.SwitchToThreadPool();
            using var decompressor = new BrotliDecompressor();
            var decompressedBuffer = decompressor.Decompress(data);
            return MemoryPackSerializer.Deserialize<T>(decompressedBuffer);
        }
    }
}
