// Author: František Holubec
// Created: 17.11.2025

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.GeoToolkit.Maps
{
    [Serializable]
    public class MeshMapSource : IMapSource
    {
        [SerializeField]
        [HideIf(nameof(_OverrideBounds))]
        [ListDrawerSettings(OnTitleBarGUI = "OnMeshFiltersTitleBarGUI")]
        private List<MeshFilter> _MeshFilters = new();

        [SerializeField]
        private bool _OverrideBounds;

        [SerializeField]
        [ShowIf(nameof(_OverrideBounds))]
        private Vector3 _BoundsMin;

        [SerializeField]
        [ShowIf(nameof(_OverrideBounds))]
        private Vector3 _BoundsMax;

        public bool IsValid => _OverrideBounds || _MeshFilters.Any(mf => mf != null && mf.sharedMesh != null);

        public void PopulateFromChildren(Transform root)
        {
            _MeshFilters = root.GetComponentsInChildren<MeshFilter>().ToList();
        }

        public MapTransformData CalculateTransformData(Transform mapTransform)
        {
            // Bounds are expressed in mapTransform's local space.
            float3 localMin;
            float3 localMax;

            if (_OverrideBounds)
            {
                localMin = _BoundsMin;
                localMax = _BoundsMax;
            }
            else
            {
                localMin = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
                localMax = new float3(float.MinValue, float.MinValue, float.MinValue);

                foreach (var meshFilter in _MeshFilters)
                {
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                        continue;

                    var bounds = meshFilter.sharedMesh.bounds;
                    var min = bounds.min;
                    var max = bounds.max;

                    var corners = new[]
                    {
                        new float3(min.x, min.y, min.z),
                        new float3(max.x, min.y, min.z),
                        new float3(min.x, max.y, min.z),
                        new float3(max.x, max.y, min.z),
                        new float3(min.x, min.y, max.z),
                        new float3(max.x, min.y, max.z),
                        new float3(min.x, max.y, max.z),
                        new float3(max.x, max.y, max.z),
                    };
                    foreach (var corner in corners)
                    {
                        float3 worldCorner = meshFilter.transform.TransformPoint(corner);
                        float3 refLocalCorner = mapTransform.InverseTransformPoint(worldCorner);

                        localMin = math.min(localMin, refLocalCorner);
                        localMax = math.max(localMax, refLocalCorner);
                    }
                }
            }

            var localSize = localMax - localMin;
            var localAxisX = new float3(localSize.x, 0f, 0f);
            var localAxisY = new float3(0f, localSize.y, 0f);
            var localAxisZ = new float3(0f, 0f, localSize.z);

            var worldOrigin = mapTransform.TransformPoint(localMin);
            var worldAxisX = mapTransform.TransformVector(localAxisX);
            var worldAxisY = mapTransform.TransformVector(localAxisY);
            var worldAxisZ = mapTransform.TransformVector(localAxisZ);

            return new MapTransformData(worldOrigin, worldAxisX, worldAxisY, worldAxisZ);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void OnMeshFiltersTitleBarGUI(InspectorProperty property)
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                if (property.Tree.WeakTargets.FirstOrDefault() is Component owner)
                    PopulateFromChildren(owner.transform);
                property.MarkSerializationRootDirty();
            }
        }
#endif
    }
}
