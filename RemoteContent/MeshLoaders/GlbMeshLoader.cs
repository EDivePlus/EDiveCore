// Author: Michal Petr
// Created: 05.05.2026

#if UNITY_GLTF
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGLTF;
using UnityGLTF.Loader;

namespace EDIVE.ServiceHub.RemoteContent.MeshLoaders
{
    [Serializable]
    public class GlbMeshLoader : IMeshLoader
    {
        [SerializeField] private bool _Multithreaded = true;
        [SerializeField] private int _MaximumLod = 300;
        [SerializeField] private int _Timeout = 8;
        [SerializeField] private GLTFSceneImporter.ColliderType _Collider = GLTFSceneImporter.ColliderType.None;
        [SerializeField] private GLTFImporterNormals _ImportNormals = GLTFImporterNormals.Import;
        [SerializeField] private GLTFImporterNormals _ImportTangents = GLTFImporterNormals.Import;
        [SerializeField] private bool _SwapUVs;
        [SerializeField] private bool _KeepCPUCopyOfMesh = true;
        [SerializeField] private bool _KeepCPUCopyOfTexture = true;

        public bool CanHandle(ContentItemInfo contentInfo) => (contentInfo?.Extension ?? string.Empty).TrimStart('.').ToLowerInvariant() == "glb";

        public async UniTask<GameObject> LoadAsync(byte[] data, Transform parent, CancellationToken cancellationToken)
        {
            if (data == null || data.Length == 0)
                throw new InvalidOperationException("GLB data is empty");
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            GLTFSceneImporter sceneImporter = null;
            ImportOptions importOptions = null;
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(data, writable: false);

                var helperHost = parent.gameObject;
                importOptions = new ImportOptions
                {
                    AsyncCoroutineHelper = helperHost.GetComponent<AsyncCoroutineHelper>()
                        ?? helperHost.AddComponent<AsyncCoroutineHelper>(),
                    DataLoader = new FileLoader(""),
                    ImportNormals = _ImportNormals,
                    ImportTangents = _ImportTangents,
                    SwapUVs = _SwapUVs,
                };

                sceneImporter = new GLTFSceneImporter(stream, importOptions)
                {
                    SceneParent = parent,
                    Collider = _Collider,
                    MaximumLod = _MaximumLod,
                    Timeout = _Timeout,
                    IsMultithreaded = _Multithreaded,
                    KeepCPUCopyOfMesh = _KeepCPUCopyOfMesh,
                    KeepCPUCopyOfTexture = _KeepCPUCopyOfTexture,
                };

                await sceneImporter.LoadSceneAsync(showSceneObj: true, cancellationToken: cancellationToken).AsUniTask();
                return sceneImporter.LastLoadedScene;
            }
            finally
            {
                sceneImporter?.Dispose();
                if (importOptions != null) importOptions.DataLoader = null;
                stream?.Dispose();
            }
        }
    }
}
#endif
