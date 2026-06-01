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

        public bool IsValid => _Terrains.Count > 0 && _Terrains.All(t => t != null && t.terrainData != null);

        public void PopulateFromChildren(Transform root)
        {
            _Terrains = root.GetComponentsInChildren<Terrain>().ToList();
        }

        public MapTransformData CalculateTransformData(Transform mapTransform)
        {
            // Terrains in unity cannot be rotated so we can just compute world min and max.
            var mapMin = new float3(float.MaxValue, float.MaxValue, float.MaxValue);
            var mapMax = new float3(float.MinValue, float.MinValue, float.MinValue);

            foreach (var terrain in _Terrains)
            {
                var terrainMin = (float3) terrain.transform.position;
                var terrainMax = terrainMin + (float3) terrain.terrainData.size;

                mapMin = math.min(mapMin, terrainMin);
                mapMax = math.max(mapMax, terrainMax);
            }

            var mapOrigin = mapMin;
            var axisX = new float3(mapMax.x - mapMin.x, 0, 0);
            var axisY = new float3(0, mapMax.y - mapMin.y, 0);
            var axisZ = new float3(0, 0, mapMax.z - mapMin.z);

            return new MapTransformData(mapOrigin, axisX, axisY, axisZ);
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
