// Author: Michal Petr
// Created: 04.05.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Procedural.MeshScaling;
using UnityEngine;

namespace EDIVE.ServiceHub.RemoteContent.Handlers
{
    public class ImageRemoteContentHandler : ARemoteContentHandler
    {
        [SerializeField]
        private MeshRenderer _QuadMesh;

        [SerializeField]
        private MeshSliceScaler _MeshSliceScaler;

        [SerializeField]
        private float _TargetSize = 1f;

        private Texture2D _texture;

        public override bool IsValidFor(ContentItemInfo contentInfo) => contentInfo.MediaTypeKey == "image";

        protected override UniTask ApplyContentAsync(RemoteContentResult content, CancellationToken cancellationToken)
        {
            if (_texture == null)
                _texture = new Texture2D(2, 2);

            if (!_texture.LoadImage(content.Bytes))
                throw new InvalidOperationException("Failed to decode image bytes");

            if (_QuadMesh != null)
                _QuadMesh.material.mainTexture = _texture;
            
            if (_MeshSliceScaler == null || _texture.width <= 0 || _texture.height <= 0)
                return UniTask.CompletedTask;

            var maxDim = Mathf.Max(_texture.width, _texture.height);
            var width = _TargetSize * _texture.width / maxDim;
            var height = _TargetSize * _texture.height / maxDim;
            _MeshSliceScaler.TargetSize = new Vector3(width, height, 1f);

            return UniTask.CompletedTask;
        }

        protected void OnDestroy()
        {
            if (_texture != null)
            {
                Destroy(_texture);
                _texture = null;
            }
        }
    }
}
