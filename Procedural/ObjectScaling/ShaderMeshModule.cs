// Author: František Holubec
// Created: 19.10.2026

using System;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.Procedural.ObjectScaling
{
    // Slices in the vertex shader, size goes to the _SliceScale material value. One mesh per shape, any size.
    // Mesh object must not be rotated relative to the scaler. Runtime uses own material instances, editor a property block.
    [Serializable]
    public class ShaderMeshModule : AObjectScalerModule
    {
        public const string SHADER_TAG = "EDIVESliced";

        private static readonly int SLICE_SCALE_ID = Shader.PropertyToID("_SliceScale");
        // Created on use, Unity objects can't be made during deserialization
        private static MaterialPropertyBlock _propertyBlock;

        // Materials serialized on renderers, never written to
        private static readonly HashSet<Material> SHARED_MATERIALS = new();

        [SerializeField]
        private Mesh _OriginalMesh;

        [SerializeField]
        private MeshFilter _MeshFilter;

        [SerializeField]
        private bool _ContributeToBounds = true;

        private Mesh _mesh;
        private Hash128 _key;
        private bool _usesPropertyBlock;
        private bool _sharedRegistered;
        private readonly List<(Material Instance, Material Source)> _instances = new();

        public override string Label => "Mesh (Shader)";

        public override bool TryCalculateBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            if (!_ContributeToBounds || _OriginalMesh == null || _MeshFilter == null)
                return false;

            GetMeshPlacement(root, out var pivot, out var meshScale);
            var meshBounds = _OriginalMesh.bounds;
            bounds = new Bounds(pivot + Vector3.Scale(meshBounds.center, meshScale), Vector3.Scale(meshBounds.size, meshScale));
            return true;
        }

        public override void SetPreviewOriginal(ObjectScaler scaler, bool preview)
        {
            if (!preview)
            {
                Recalculate(scaler, true);
                return;
            }

            if (_MeshFilter == null)
                return;
            _MeshFilter.sharedMesh = _OriginalMesh;
            if (_MeshFilter.TryGetComponent<Renderer>(out var meshRenderer))
            {
                meshRenderer.ResetLocalBounds();
                ApplyScale(meshRenderer, Vector3.one);
            }
        }

        public override void Recalculate(ObjectScaler scaler, bool force = false)
        {
            if (_OriginalMesh == null || _MeshFilter == null || !IsValidHierarchy(scaler))
            {
                Release(scaler);
                return;
            }

            var details = scaler.Details;
            GetMeshPlacement(scaler.transform, out var pivot, out var meshScale);

            var key = details.GetShapeHash();
            key.Append(_OriginalMesh.GetInstanceID());
            ObjectScaleDetails.Append(ref key, pivot, 0.0001f);
            ObjectScaleDetails.Append(ref key, meshScale, 0.0001f);

            if (_mesh == null || key != _key)
            {
                ScaledMeshCache.Release(_mesh);
                _key = key;
                _mesh = ScaledMeshCache.Acquire(key, _OriginalMesh, mesh => SliceMath.BakeShaderMesh(_OriginalMesh, mesh, details, pivot, meshScale));
            }
            else if (force)
            {
                ScaledMeshCache.Rebuild(_mesh, mesh => SliceMath.BakeShaderMesh(_OriginalMesh, mesh, details, pivot, meshScale));
            }

            if (_MeshFilter.sharedMesh != _mesh)
                _MeshFilter.sharedMesh = _mesh;

            if (!_MeshFilter.TryGetComponent<Renderer>(out var meshRenderer))
                return;

            var bounds = SliceMath.ScaledBounds(details);
            var localBounds = new Bounds((bounds.center - pivot).DivideElementWise(meshScale), bounds.size.DivideElementWise(meshScale));
            if (meshRenderer.localBounds != localBounds)
                meshRenderer.localBounds = localBounds;

            ApplyScale(meshRenderer, details.Scale);
        }

        public override void Release(ObjectScaler scaler)
        {
            ScaledMeshCache.Release(_mesh);
            _mesh = null;

            if (_MeshFilter != null && _MeshFilter.TryGetComponent<Renderer>(out var meshRenderer))
            {
                RestoreSourceMaterials(meshRenderer);
                ClearPropertyBlock(meshRenderer);
            }

            DestroyInstances();
        }

        private void ApplyScale(Renderer meshRenderer, Vector3 scale)
        {
            var value = new Vector4(scale.x, scale.y, scale.z, 1);
            if (!Application.isPlaying)
            {
                RestoreSourceMaterials(meshRenderer);
                DestroyInstances();
                _propertyBlock ??= new MaterialPropertyBlock();
                meshRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetVector(SLICE_SCALE_ID, value);
                meshRenderer.SetPropertyBlock(_propertyBlock);
                _usesPropertyBlock = true;
                return;
            }

            ClearPropertyBlock(meshRenderer);
            var materials = meshRenderer.sharedMaterials;
            if (!_sharedRegistered)
            {
                foreach (var material in materials)
                {
                    if (material != null && !IsOwned(material))
                        SHARED_MATERIALS.Add(material);
                }

                _sharedRegistered = true;
            }

            var changed = false;
            for (var i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (material == null || string.IsNullOrEmpty(material.GetTag(SHADER_TAG, false)))
                    continue;

                // Runtime instances made by others (e.g. a photo texture) are written directly, shared ones get own instance
                if (!IsOwned(material) && IsShared(material))
                {
                    var instance = new Material(material) {name = $"{material.name} (sliced)", hideFlags = HideFlags.DontSave};
                    _instances.Add((instance, material));
                    materials[i] = instance;
                    material = instance;
                    changed = true;
                }

                material.SetVector(SLICE_SCALE_ID, value);
            }

            if (changed)
                meshRenderer.sharedMaterials = materials;

            DestroyUnusedInstances(materials);
        }

        private bool IsOwned(Material material)
        {
            foreach (var (instance, _) in _instances)
            {
                if (instance == material)
                    return true;
            }

            return false;
        }

        private static bool IsShared(Material material)
        {
#if UNITY_EDITOR
            if (EditorUtility.IsPersistent(material))
                return true;
#endif
            return SHARED_MATERIALS.Contains(material);
        }

        private void RestoreSourceMaterials(Renderer meshRenderer)
        {
            if (_instances.Count == 0)
                return;

            var materials = meshRenderer.sharedMaterials;
            var changed = false;
            for (var i = 0; i < materials.Length; i++)
            {
                foreach (var (instance, source) in _instances)
                {
                    if (materials[i] != instance)
                        continue;
                    materials[i] = source;
                    changed = true;
                }
            }

            if (changed)
                meshRenderer.sharedMaterials = materials;
        }

        private void DestroyUnusedInstances(Material[] materials)
        {
            for (var i = _instances.Count - 1; i >= 0; i--)
            {
                if (Array.IndexOf(materials, _instances[i].Instance) >= 0)
                    continue;
                DestroyMaterial(_instances[i].Instance);
                _instances.RemoveAt(i);
            }
        }

        private void DestroyInstances()
        {
            foreach (var (instance, _) in _instances)
                DestroyMaterial(instance);
            _instances.Clear();
        }

        private static void DestroyMaterial(Material material)
        {
            if (material == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(material);
            else
                Object.DestroyImmediate(material);
        }

        private void ClearPropertyBlock(Renderer meshRenderer)
        {
            if (!_usesPropertyBlock)
                return;
            meshRenderer.SetPropertyBlock(null);
            _usesPropertyBlock = false;
        }

        private void GetMeshPlacement(Transform root, out Vector3 pivot, out Vector3 meshScale)
        {
            var matrix = root.worldToLocalMatrix * _MeshFilter.transform.localToWorldMatrix;
            pivot = matrix.GetColumn(3);
            meshScale = new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);
        }

        private bool IsValidHierarchy(ObjectScaler scaler)
        {
            var tr = _MeshFilter.transform;
            return tr.IsChildOf(scaler.transform) && Quaternion.Angle(scaler.transform.rotation, tr.rotation) < 0.01f;
        }

        public override string GetWarning(ObjectScaler scaler)
        {
            if (_MeshFilter == null)
                return null;
            if (!IsValidHierarchy(scaler))
                return "Mesh object must be under the scaler without rotation.";
            if (_OriginalMesh != null && !_OriginalMesh.isReadable)
                return $"{_OriginalMesh.name} is not readable, enable Read/Write in import settings.";
            if (_MeshFilter.TryGetComponent<Renderer>(out var meshRenderer))
            {
                foreach (var material in meshRenderer.sharedMaterials)
                {
                    if (material != null && string.IsNullOrEmpty(material.GetTag(SHADER_TAG, false)))
                        return $"Material {material.name} does not use a sliced shader.";
                }
            }

            if (_MeshFilter.GetComponent<Collider>() != null)
                return "Collider on the mesh object ignores slicing, use Box Collider module on another object.";
#if UNITY_EDITOR
            if (GameObjectUtility.GetStaticEditorFlags(_MeshFilter.gameObject).HasFlag(StaticEditorFlags.ContributeGI))
                return "Contribute GI is on, baked lighting ignores slicing.";
#endif
            return null;
        }
    }
}
