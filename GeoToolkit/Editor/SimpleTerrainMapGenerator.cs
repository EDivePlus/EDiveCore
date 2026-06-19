// Author: František Holubec
// Created: 02.05.2025

#if UNITY_EDITOR
using System.Linq;
using EDIVE.GeoToolkit.Utils;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace EDIVE.GeoToolkit.TerrainTools
{
    public class SimpleTerrainMapGenerator : MonoBehaviour
    {
        [SerializeField]
        private Texture2D _HeightMap;

        [SerializeField]
        private float3 _Size;

        [SerializeField]
        private bool _DrawHoles;

        [SerializeField]
        private Material _Material;

        [Button]
        private void GenerateUnityTerrains()
        {
            var heightMap = _HeightMap.LoadGrayScale();

            var heightMapFlat = heightMap.To1DArray();
            var nonZeroValues = heightMapFlat.Where(i => i != 0);
            var terrainMax = heightMapFlat.Max();
            var terrainMin = nonZeroValues.Min();
            var terrainHeight = terrainMax - terrainMin;

            var textureSize = new int2(_HeightMap.width, _HeightMap.height);
            var heightmapResolution = math.max(textureSize.x, textureSize.y);
            var terrainData = new TerrainData
            {
                heightmapResolution = heightmapResolution,
                size = new Vector3(_Size.x, terrainHeight, _Size.z)
            };

            if (_DrawHoles)
            {
                var holeMap = heightMap.Resize2D(heightmapResolution - 1, heightmapResolution - 1)
                    .Apply((array, i, j) =>
                    {
                        for (var xi = -1; xi <= 1; xi++)
                        {
                            for (var yi = -1; yi <= 1; yi++)
                            {
                                var newX = i + xi;
                                var newY = j + yi;
                                if (array.IsValid2DCoordinate(newX, newY) && array[newX, newY].Approximately(0))
                                    return false;
                            }
                        }

                        return true;
                    });
                terrainData.SetHoles(0, 0, holeMap);
            }

            heightMap.Remap(terrainMin, terrainMax, 0, 1);
            heightMap.Clamp(0, 1);
            heightMap = heightMap.Transpose().ExtendByOne();
            if (heightMap.GetLength(0) == 0 || heightMap.GetLength(1) == 0)
            {
                Debug.LogError("Invalid Terrain");
                return;
            }

            terrainData.SetHeights(0, 0, heightMap);

            var terrainDataPath = "Assets/TerrainData.asset";
            AssetDatabase.CreateAsset(terrainData, terrainDataPath);
            terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainDataPath);

            var terrain = Terrain.CreateTerrainGameObject(terrainData).GetComponent<Terrain>();
            terrain.materialTemplate = _Material;
            terrain.gameObject.name = "Terrain";
            var terrainTr = terrain.transform;
            terrainTr.SetParent(transform);
            terrainTr.Reset();
        }
        
        [Button]
        public float GetHeightMapFromTexture()
        {
            var heightMap = _HeightMap.LoadGrayScale();

            var heightMapFlat = heightMap.To1DArray();
            var nonZeroValues = heightMapFlat.Where(i => i != 0);
            var terrainMax = heightMapFlat.Max();
            var terrainMin = nonZeroValues.Min();
            return terrainMax - terrainMin;
        }
    }
}
#endif
