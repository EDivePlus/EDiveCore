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
        [Tooltip("Optional MeshScaler to apply scaling to the image based on its pixel dimensions and the PixelsPerMeter setting.")]
        private MeshSliceScaler _MeshSliceScaler;

        [SerializeField]
        private float _PixelsPerMeter = 1024f;

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

            if (_MeshSliceScaler == null || !(_PixelsPerMeter > 0f)) 
                return UniTask.CompletedTask;
            
            var width = _texture.width / _PixelsPerMeter;
            var height = _texture.height / _PixelsPerMeter;
            _MeshSliceScaler.TargetSize = new Vector3(width, height, _MeshSliceScaler.Size.z);

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
