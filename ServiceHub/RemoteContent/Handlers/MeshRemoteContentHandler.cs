// Author: Michal Petr
// Created: 04.05.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.ServiceHub.RemoteContent.MeshLoaders;
using UnityEngine;
using ZLinq;

namespace EDIVE.ServiceHub.RemoteContent.Handlers
{
    public class MeshRemoteContentHandler : ARemoteContentHandler
    {
        [SerializeField]
        private Transform _Root;

        [SerializeField]
        private float _TargetSize = 1f;

        [SerializeReference]
        private List<IMeshLoader> _Loaders = new();

        private GameObject _spawned;

        public override bool IsValidFor(ContentItemInfo contentInfo)
        {
            return contentInfo.MediaTypeKey == "model" && _Loaders.AsValueEnumerable().Any(l => l != null && l.CanHandle(contentInfo));
        }

        protected override async UniTask ApplyContentAsync(RemoteContentResult content, CancellationToken cancellationToken)
        {
            var loader = _Loaders.AsValueEnumerable().FirstOrDefault(l => l != null && l.CanHandle(ContentInfo));
            if (loader == null)
                throw new NotSupportedException($"No loader found for content '{ContentInfo.Name}'");

            DestroySpawned();

            var root = _Root != null ? _Root : transform;
            var spawned = await loader.LoadAsync(content.Bytes, root, cancellationToken);
            if (spawned == null)
                throw new InvalidOperationException($"Loader '{loader.GetType().Name}' returned null GameObject");

            _spawned = spawned;
            FitToBox(spawned.transform, root, _TargetSize);
        }

        private void DestroySpawned()
        {
            if (_spawned == null)
                return;
            Destroy(_spawned);
            _spawned = null;
        }

        private static void FitToBox(Transform target, Transform root, float size)
        {
            target.SetParent(root, false);
            target.localPosition = Vector3.zero;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;

            if (!TryGetWorldBounds(target, out var bounds))
                return;

            var maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxDim <= Mathf.Epsilon)
                return;

            target.localScale = Vector3.one * (size / maxDim);

            if (!TryGetWorldBounds(target, out var scaledBounds))
                return;
            target.position += root.position - scaledBounds.center;
        }

        private static bool TryGetWorldBounds(Transform target, out Bounds bounds)
        {
            var renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return true;
        }

        private void OnDestroy()
        {
            DestroySpawned();
        }
    }
}
