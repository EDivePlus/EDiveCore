// Author: František Holubec
// Created: 22.10.2025

using System;
using System.IO;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.SerializedTypeMigration;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Procedural.ObjectScaling
{
    // Slices on CPU, works with any shader and mesh collider. Meshes are shared per size.
    [Serializable]
    [FormerlySerializedType(0, "MeshComponent", "EDIVE.Procedural.MeshScaling")]
    public class MeshModule : AObjectScalerModule
    {
        private const int STREAK_FRAMES = 3;

        [SerializeField]
        private Mesh _OriginalMesh;

        [SerializeField]
        private MeshFilter _MeshFilter;

        [SerializeField]
        private MeshCollider _MeshCollider;

        [SerializeField]
        private bool _ContributeToBounds = true;

        [InlineIconButton(FontAwesomeEditorIconType.FloppyDiskSolid, "SaveTargetMesh")]
        [ShowInInspector]
        [ReadOnly]
        private Mesh _targetMesh;

        private Hash128 _key;
        private bool _ownsMesh;
        private int _lastFrame = -10;
        private int _streak;
        private bool _warned;

        public override string Label => "Mesh (CPU)";

        public override bool TryCalculateBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            if (!_ContributeToBounds || _OriginalMesh == null || !TryGetTransform(out var tr))
                return false;

            bounds = MeshUtility.CalculateBounds(_OriginalMesh, tr, root);
            return true;
        }

        private bool TryGetTransform(out Transform tr)
        {
            tr = _MeshFilter != null ? _MeshFilter.transform : _MeshCollider != null ? _MeshCollider.transform : null;
            return tr != null;
        }

        public override void SetPreviewOriginal(ObjectScaler scaler, bool preview)
        {
            if (preview)
                Assign(_OriginalMesh);
            else
                Recalculate(scaler, true);
        }

        public override void Recalculate(ObjectScaler scaler, bool force = false)
        {
            if (_OriginalMesh == null || !TryGetTransform(out var tr))
            {
                Release(scaler);
                return;
            }

            var details = scaler.Details;
            var matrix = MeshUtility.ChildToRootMatrix(tr, scaler.transform);

            // Resized every frame, a cached mesh per step would pile up
            var frame = Time.frameCount;
            if (frame != _lastFrame)
                _streak = frame - _lastFrame == 1 ? _streak + 1 : 0;
            _lastFrame = frame;
            if (_streak >= STREAK_FRAMES && Application.isPlaying)
            {
                if (!_warned)
                {
                    _warned = true;
                    Debug.LogWarning($"[ObjectScaler] {scaler.name} resized every frame on CPU mesh, use {nameof(ShaderMeshModule)}.", scaler);
                }

                if (!_ownsMesh)
                {
                    ReleaseMesh();
                    _targetMesh = ScaledMeshCache.CreateCopy(_OriginalMesh, "(sliced)");
                    _ownsMesh = true;
                }
                SliceMath.ScaleMesh(_OriginalMesh, _targetMesh, details, matrix);
                Assign(_targetMesh);
                return;
            }

            var key = GetKey(details, matrix);
            if (_ownsMesh || _targetMesh == null || key != _key)
            {
                ReleaseMesh();
                _key = key;
                _targetMesh = ScaledMeshCache.Acquire(key, _OriginalMesh, mesh => SliceMath.ScaleMesh(_OriginalMesh, mesh, details, matrix));
            }
            else if (force)
            {
                ScaledMeshCache.Rebuild(_targetMesh, mesh => SliceMath.ScaleMesh(_OriginalMesh, mesh, details, matrix));
            }

            Assign(_targetMesh);
        }

        public override void Release(ObjectScaler scaler)
        {
            ReleaseMesh();
        }

        private Hash128 GetKey(ObjectScaleDetails details, Matrix4x4 matrix)
        {
            var hash = details.GetShapeHash();
            hash.Append(_OriginalMesh.GetInstanceID());
            ObjectScaleDetails.Append(ref hash, details.Size);
            ObjectScaleDetails.Append(ref hash, matrix.GetColumn(0), 0.0001f);
            ObjectScaleDetails.Append(ref hash, matrix.GetColumn(1), 0.0001f);
            ObjectScaleDetails.Append(ref hash, matrix.GetColumn(2), 0.0001f);
            ObjectScaleDetails.Append(ref hash, matrix.GetColumn(3), 0.0001f);
            return hash;
        }

        private void Assign(Mesh mesh)
        {
            if (_MeshFilter != null && _MeshFilter.sharedMesh != mesh)
                _MeshFilter.sharedMesh = mesh;
            if (_MeshCollider != null && _MeshCollider.sharedMesh != mesh)
                _MeshCollider.sharedMesh = mesh;
        }

        private void ReleaseMesh()
        {
            if (_targetMesh == null)
            {
                _ownsMesh = false;
                return;
            }

#if UNITY_EDITOR
            // Saved asset, keep
            if (UnityEditor.EditorUtility.IsPersistent(_targetMesh))
            {
                _targetMesh = null;
                _ownsMesh = false;
                return;
            }
#endif
            if (_ownsMesh)
                ScaledMeshCache.DestroyMesh(_targetMesh);
            else
                ScaledMeshCache.Release(_targetMesh);

            _targetMesh = null;
            _ownsMesh = false;
        }

        public override string GetWarning(ObjectScaler scaler)
        {
            if (_OriginalMesh != null && !_OriginalMesh.isReadable)
                return $"{_OriginalMesh.name} is not readable, enable Read/Write in import settings.";
            return null;
        }

#if UNITY_EDITOR
        private void SaveTargetMesh()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (_targetMesh == null)
                    return;

                var originalPath = Directory.GetParent(PathUtility.GetAbsolutePath(UnityEditor.AssetDatabase.GetAssetPath(_OriginalMesh)))?.FullName;
                var path = UnityEditor.EditorUtility.SaveFilePanelInProject("Save Sliced Mesh", _targetMesh.name, "mesh", "Select location to save the sliced mesh.", originalPath);
                if (string.IsNullOrEmpty(path))
                    return;

                var copy = UnityEngine.Object.Instantiate(_targetMesh);
                copy.hideFlags = HideFlags.None;
                UnityEditor.AssetDatabase.CreateAsset(copy, path);
                UnityEditor.AssetDatabase.SaveAssets();
            };
        }
#endif
    }
}
