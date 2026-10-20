// Author: Michal Petr
// Created: 04.05.2026

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Procedural.ObjectScaling;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.ServiceHub.RemoteContent.Handlers
{
    public class ImageRemoteContentHandler : ARemoteContentHandler
    {
        [SerializeField]
        [FormerlySerializedAs("_QuadMesh")]
        private Renderer _Renderer;

        [SerializeField]
        [MinValue(0)]
        [Tooltip("Index of the material the image is assigned to.")]
        private int _MaterialIndex;

        [SerializeField]
        [FormerlySerializedAs("_MeshSliceScaler")]
        private ObjectScaler _Scaler;

        [SerializeField]
        [Tooltip("Scaled to fit max size, keeps image ratio.")]
        private Vector2 _MaxSize = Vector2.one;

        private Texture2D _texture;
        private Material _material;

        public override bool IsValidFor(ContentItemInfo contentInfo) => contentInfo.MediaTypeKey == "image";

        protected override UniTask ApplyContentAsync(RemoteContentResult content, CancellationToken cancellationToken)
        {
            if (_texture == null)
                _texture = new Texture2D(2, 2);

            if (!_texture.LoadImage(content.Bytes))
                throw new InvalidOperationException("Failed to decode image bytes");

            if (TryGetMaterialInstance(out var material))
                material.mainTexture = _texture;

            if (_Scaler != null)
                _Scaler.SetSizeXY(ObjectScaler.FitAspect(_MaxSize, _texture.width, _texture.height));

            return UniTask.CompletedTask;
        }

        // Instances only the target material, the rest stay shared.
        private bool TryGetMaterialInstance(out Material material)
        {
            material = _material;
            if (material != null)
                return true;
            if (_Renderer == null)
                return false;

            var materials = _Renderer.sharedMaterials;
            if (_MaterialIndex >= materials.Length || materials[_MaterialIndex] == null)
            {
                Debug.LogError($"[ImageRemoteContentHandler] No material at index {_MaterialIndex}", this);
                return false;
            }

            _material = new Material(materials[_MaterialIndex]);
            materials[_MaterialIndex] = _material;
            _Renderer.sharedMaterials = materials;
            material = _material;
            return true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_texture != null)
            {
                Destroy(_texture);
                _texture = null;
            }

            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }
        }
    }
}
