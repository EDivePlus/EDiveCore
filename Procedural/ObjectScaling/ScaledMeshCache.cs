// Author: František Holubec
// Created: 19.10.2026

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace EDIVE.Procedural.ObjectScaling
{
    // Shared scaled meshes, one per key, destroyed with the last user.
    public static class ScaledMeshCache
    {
        private class Entry
        {
            public Hash128 Key;
            public Mesh Mesh;
            public Mesh Source;
            public Action<Mesh> Build;
            public int Users;
        }

        private static readonly Dictionary<Hash128, Entry> ENTRIES = new();
        private static readonly Dictionary<Mesh, Entry> BY_MESH = new();

        public static int Count => ENTRIES.Count;

        public static Mesh Acquire(Hash128 key, Mesh source, Action<Mesh> build)
        {
            if (ENTRIES.TryGetValue(key, out var entry) && entry.Mesh != null)
            {
                entry.Users++;
                return entry.Mesh;
            }

            var mesh = CreateCopy(source, "(scaled)");
            build(mesh);
            entry = new Entry {Key = key, Mesh = mesh, Source = source, Build = build, Users = 1};
            ENTRIES[key] = entry;
            BY_MESH[mesh] = entry;
            return mesh;
        }

        public static void Rebuild(Mesh mesh, Action<Mesh> build)
        {
            if (mesh == null || !BY_MESH.TryGetValue(mesh, out var entry))
                return;

            entry.Build = build;
            Rebuild(entry);
        }

        // Source topology could have changed, copy it again before building
        private static void Rebuild(Entry entry)
        {
            if (entry.Source == null || entry.Mesh == null)
                return;

            CopyMeshData(entry.Source, entry.Mesh);
            entry.Build(entry.Mesh);
        }

        private static void CopyMeshData(Mesh source, Mesh target)
        {
            using var sourceArray = Mesh.AcquireReadOnlyMeshData(source);
            var sourceData = sourceArray[0];
            var targetArray = Mesh.AllocateWritableMeshData(1);
            var targetData = targetArray[0];

            targetData.SetVertexBufferParams(sourceData.vertexCount, source.GetVertexAttributes());
            for (var stream = 0; stream < sourceData.vertexBufferCount; stream++)
                targetData.GetVertexData<byte>(stream).CopyFrom(sourceData.GetVertexData<byte>(stream));

            var indices = sourceData.GetIndexData<byte>();
            var indexSize = sourceData.indexFormat == IndexFormat.UInt16 ? 2 : 4;
            targetData.SetIndexBufferParams(indices.Length / indexSize, sourceData.indexFormat);
            targetData.GetIndexData<byte>().CopyFrom(indices);

            const MeshUpdateFlags flags = MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices;
            targetData.subMeshCount = sourceData.subMeshCount;
            for (var i = 0; i < sourceData.subMeshCount; i++)
                targetData.SetSubMesh(i, sourceData.GetSubMesh(i), flags);

            Mesh.ApplyAndDisposeWritableMeshData(targetArray, target, flags);
        }

        public static void Release(Mesh mesh)
        {
            if (mesh is null || !BY_MESH.TryGetValue(mesh, out var entry))
                return;

            if (--entry.Users > 0)
                return;

            BY_MESH.Remove(mesh);
            ENTRIES.Remove(entry.Key);
            DestroyMesh(mesh);
        }

        public static bool IsCached(Mesh mesh) => mesh != null && BY_MESH.ContainsKey(mesh);

        public static Mesh CreateCopy(Mesh source, string suffix)
        {
            var mesh = Object.Instantiate(source);
            mesh.name = $"{source.name} {suffix}";
            mesh.hideFlags = HideFlags.DontSave;
            return mesh;
        }

        public static void DestroyMesh(Mesh mesh)
        {
            if (mesh == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(mesh);
            else
                Object.DestroyImmediate(mesh);
        }

        private static void Clear()
        {
            foreach (var entry in ENTRIES.Values)
                DestroyMesh(entry.Mesh);
            ENTRIES.Clear();
            BY_MESH.Clear();
        }

#if UNITY_EDITOR
        // Managed refs die with the domain, meshes would leak
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterReloadCleanup()
        {
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= Clear;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += Clear;
        }

        // Reimported source keeps its instance, so keys still match
        private class SourceReimportProcessor : UnityEditor.AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                if (ENTRIES.Count == 0)
                    return;

                var paths = new HashSet<string>(imported);
                foreach (var entry in ENTRIES.Values)
                {
                    if (entry.Source != null && paths.Contains(UnityEditor.AssetDatabase.GetAssetPath(entry.Source)))
                        Rebuild(entry);
                }
            }
        }
#endif
    }
}
