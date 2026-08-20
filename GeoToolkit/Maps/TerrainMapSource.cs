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
    public class TerrainMapSource : IMapSource
    {
        [SerializeField]
        [ListDrawerSettings(OnTitleBarGUI = "OnTerrainsTitleBarGUI")]
        private List<Terrain> _Terrains = new();

        [SerializeField]
        private Quaternion _Rotation = Quaternion.identity;

        public bool IsValid => _Terrains.Count > 0 && _Terrains.All(t => t != null && t.terrainData != null);

        public void PopulateFromChildren(Transform root)
        {
            _Terrains = root.GetComponentsInChildren<Terrain>().ToList();
        }

        public MapTransformData CalculateTransformData(Transform mapTransform)
        {
            // Terrains cannot be rotated, so each one is an axis-aligned box in world space.
            // Bounds are accumulated in the frame of _Rotation so the resulting box is aligned to it.
            var rotation = (quaternion) _Rotation;
            var inverseRotation = math.inverse(rotation);

            var localMin = new float3(float.MaxValue);
            var localMax = new float3(float.MinValue);

            foreach (var terrain in _Terrains)
            {
                var worldMin = (float3) terrain.transform.position;
                var worldMax = worldMin + (float3) terrain.terrainData.size;

                for (var i = 0; i < 8; i++)
                {
                    var corner = math.select(worldMin, worldMax, new bool3((i & 1) != 0, (i & 2) != 0, (i & 4) != 0));
                    var localCorner = math.mul(inverseRotation, corner);
                    localMin = math.min(localMin, localCorner);
                    localMax = math.max(localMax, localCorner);
                }
            }

            var localSize = localMax - localMin;
            var origin = math.mul(rotation, localMin);
            var axisX = math.mul(rotation, new float3(localSize.x, 0, 0));
            var axisY = math.mul(rotation, new float3(0, localSize.y, 0));
            var axisZ = math.mul(rotation, new float3(0, 0, localSize.z));

            return new MapTransformData(origin, axisX, axisY, axisZ);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void OnTerrainsTitleBarGUI(InspectorProperty property)
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
