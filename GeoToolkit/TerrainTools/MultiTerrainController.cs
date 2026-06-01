using System.Collections.Generic;
using System.Linq;
using EDIVE.DataStructures;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEngine;

namespace EDIVE.GeoToolkit.TerrainTools
{
    public class MultiTerrainController : MonoBehaviour
    {
        [InfoBox("There are Null values in the terrain list, you may want to refresh it!", InfoMessageType.Warning, "AnyNullTerrains")]
        [InfoBox("No terrains in the list, you may want to refresh it!", InfoMessageType.Warning, "NoTerrains")]
        [SerializeField]
        [EnhancedBoxGroup("Terrains", SpaceAfter = 8)]
        [HideLabel]
        [PropertyOrder(-10)]
        private Serializable2DArray<Terrain> terrains;
        
        public Serializable2DArray<Terrain> Terrains => terrains;

        private float3 GetTerrainOrigin()
        {
            if (terrains.TryGetElement(0, 0, out var element) && element != null)
            {
                return element.transform.position;
            }

            return float3.zero;
        }

        private float3 GetTerrainMaxPoint()
        {
            var lastTerrain = terrains[terrains.Width - 1, terrains.Height - 1];
            if (lastTerrain != null)
            {
                return (float3) lastTerrain.transform.position + (float3) lastTerrain.terrainData.size;
            }

            return float3.zero;
        }

        [SerializeField]
        [HideInInspector]
        private int pixelError = 5;

        [PropertySpace]
        [ShowInInspector]
        [PropertyRange(1, 200)]
        [PropertyTooltip("The accuracy of the mapping between Terrain maps (such as heightmaps and Textures) and generated Terrain. " +
                         "Higher values indicate lower accuracy, but with lower rendering overhead.")]
        private int PixelError
        {
            get => pixelError;
            set
            {
                pixelError = value;
                foreach (var terrain in terrains)
                {
                    terrain.heightmapPixelError = pixelError;
                }
            }
        }

        [SerializeField]
        [HideInInspector]
        private int baseMapDistance = 1000;

        [ShowInInspector]
        [PropertyRange(0, 20000)]
        [PropertyTooltip("The maximum distance at which Unity displays Terrain Textures at full resolution. " +
                         "Beyond this distance, the system uses a lower resolution composite image for efficiency.")]
        private int BaseMapDistance
        {
            get => baseMapDistance;
            set
            {
                baseMapDistance = value;
                foreach (var terrain in terrains)
                {
                    terrain.basemapDistance = baseMapDistance;
                }
            }
        }

        [SerializeField]
        [PropertyOrder(-10)]
        [PropertyTooltip("The size of the whole terrain in the world coordinates\nWidth and height range 1 - 100 000\nHeight ranges 1 - 10 000")]
        private float3 terrainSize;

        [PropertyOrder(-10)]
        [Button]
        private void ApplyTerrainSize()
        {
            var origin = GetTerrainOrigin();

            var xSize = terrainSize.x / terrains.Width;
            var zSize = terrainSize.z / terrains.Height;
            var ySize = terrainSize.y;

            for (var x = 0; x < terrains.Width; x++)
            {
                for (var z = 0; z < terrains.Height; z++)
                {
                    var terrain = terrains[x, z];
                    var terrainData = terrain.terrainData;
                    terrainData.size = new Vector3(xSize, ySize, zSize);
                    var terrainTr = terrain.transform;
                    terrainTr.position = new Vector3(origin.x + xSize * x, terrainTr.position.y, origin.z + zSize * z);
                }
            }
        }

        [PropertyOrder(-10)]
        [Button]
        private void ScaleTerrain(float multiplier = 1)
        {
            for (var x = 0; x < terrains.Width; x++)
            {
                for (var z = 0; z < terrains.Height; z++)
                {
                    var terrain = terrains[x, z];
                    var terrainData = terrain.terrainData;
                    terrainData.size *= multiplier;
                    terrain.transform.position *= multiplier;
                }
            }
        }

        [SerializeField] 
        [HideInInspector] 
        private int detailResolutionPerPatch = 32;

        [ShowInInspector]
        [PropertyRange(8, 128)]
        [PropertyTooltip("The number of cells in a single patch (mesh). " +
                         "This value is squared to form a grid of cells, and must be a divisor of the detail resolution.")]
        private int DetailResolutionPerPatch
        {
            get => detailResolutionPerPatch;
            set
            {
                detailResolutionPerPatch = value;
                foreach (var terrain in terrains)
                {
                    terrain.terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch);
                }
            }
        }

        [SerializeField] 
        [HideInInspector]
        private int detailResolution = 1024;

        [ShowInInspector]
        [PropertyRange(0, 4048)]
        [PropertyTooltip("The number of cells available for placing details onto the Terrain tile. " +
                         "This value is squared to make a grid of cells.")]
        private int DetailResolution
        {
            get => detailResolution;
            set
            {
                detailResolution = value;
                foreach (var terrain in terrains)
                {
                    terrain.terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch);
                }
            }
        }
        
        [SerializeField] 
        [HideInInspector] 
        private int heightMapResolution = 1025;

        [PropertySpace]
        [ShowInInspector]
        [ValueDropdown("GetHeightMapResolutions")]
        [PropertyTooltip("The pixel resolution of the Terrain’s heightmap.")]
        private int HeightMapResolution
        {
            get => heightMapResolution;
            set
            {
                if (!MathExtensions.IsPowerOfTwo(value - 1))
                {
                    Debug.LogError("Value is not power of two plus one!");
                    return;
                }
                heightMapResolution = value;
                foreach (var terrain in terrains)
                {
                    ResizeHeightmap(terrain, value);
                }
            }
        }
        
        [SerializeField] 
        [HideInInspector] 
        private int controlTextureResolution = 512;

        [ShowInInspector]
        [ValueDropdown("GetTextureResolutions")]
        [PropertyTooltip("The resolution of the splatmap that controls the blending of the different Terrain Textures.")]
        private int ControlTextureResolution
        {
            get => controlTextureResolution;
            set
            {
                if (!MathExtensions.IsPowerOfTwo(value))
                {
                    Debug.LogError("Value is not power of two!");
                    return;
                }
                controlTextureResolution = value;
                foreach (var terrain in terrains)
                {
                    terrain.terrainData.alphamapResolution = value;
                }
            }
        }
        
        [SerializeField] 
        [HideInInspector] 
        private int baseTextureResolution = 512;

        [ShowInInspector]
        [ValueDropdown("GetTextureResolutions")]
        [PropertyTooltip("The resolution of the composite Texture to use on the Terrain when you view it from a distance greater than the Basemap Distance.")]
        private int BaseTextureResolution
        {
            get => baseTextureResolution;
            set
            {
                if (!MathExtensions.IsPowerOfTwo(value))
                {
                    Debug.LogError("Value is not power of two!");
                    return;
                }
                baseTextureResolution = value;
                foreach (var terrain in terrains)
                {
                    terrain.terrainData.baseMapResolution = value;
                }
            }
        }

        [PropertySpace]
        [Button]
        private void ScanTerrainData()
        {
            if(terrains.Count == 0) return;
            var terrain = terrains[0];
            var terrainData = terrain.terrainData;
            
            
            pixelError = terrain.heightmapPixelError.RoundToInt();
            baseMapDistance = terrain.basemapDistance.RoundToInt();
            detailResolutionPerPatch = terrainData.detailResolutionPerPatch;
            detailResolution = terrainData.detailResolution;
            terrainSize = GetTerrainMaxPoint() - GetTerrainOrigin();
        }
        
        [PropertySpace]
        [Button]
        private void ResetAllToDefaults()
        {
            pixelError = 5;
            baseMapDistance = 1000;
            detailResolutionPerPatch = 32;
            detailResolution = 1024;
            heightMapResolution = 1025;
            controlTextureResolution = 512;
            baseTextureResolution = 512;
            
            foreach (var terrain in terrains)
            {
                var terrainData = terrain.terrainData;
                terrain.heightmapPixelError = pixelError;
                terrain.basemapDistance = baseMapDistance;
                terrainData.SetDetailResolution(detailResolution, detailResolutionPerPatch);
                terrainData.baseMapResolution = baseTextureResolution;
                terrainData.alphamapResolution = controlTextureResolution;
                ResizeHeightmap(terrain, heightMapResolution);
            }
        }

        private static void ResizeHeightmap(Terrain terrain, int newResolution)
        {
            var oldRT = RenderTexture.active;
            var terrainData = terrain.terrainData;

            var oldHeightmap = RenderTexture.GetTemporary(terrainData.heightmapTexture.descriptor);
            Graphics.Blit(terrainData.heightmapTexture, oldHeightmap);
            
            var dWidth = terrainData.heightmapResolution;
            var sWidth = newResolution;

            var oldSize = terrainData.size;
            terrainData.heightmapResolution = newResolution;
            terrainData.size = oldSize;

            oldHeightmap.filterMode = FilterMode.Bilinear;
            
            var k = (dWidth - 1.0f) / (sWidth - 1.0f) / dWidth;
            var scaleX = (sWidth * k);
            var offsetX = (float)(0.5 / dWidth - 0.5 * k);
            var scale = new float2(scaleX, scaleX);
            var offset = new float2(offsetX, offsetX);

            Graphics.Blit(oldHeightmap, terrain.terrainData.heightmapTexture, scale, offset);
            RenderTexture.ReleaseTemporary(oldHeightmap);

            RenderTexture.active = oldRT;

            terrain.terrainData.DirtyHeightmapRegion(new RectInt(0, 0, terrain.terrainData.heightmapTexture.width, terrainData.heightmapTexture.height), TerrainHeightmapSyncControl.HeightAndLod);
        }
        
        private static Serializable2DArray<Terrain> SortArraysToGrid(IEnumerable<Terrain> terrainList)
        {
            var gridPrototype = terrainList
                .GroupBy(t => t.transform.position.x)
                .Select(g => g
                    .GroupBy(t => t.transform.position.z)
                    .SelectMany(ts => ts)
                    .ToList())
                .ToList();

            return new Serializable2DArray<Terrain>(gridPrototype);
        }

#if UNITY_EDITOR
        [EnhancedBoxGroup("Terrains")]
        [Button("Refresh")]
        public void RefreshTerrains()
        {
            terrains = SortArraysToGrid(GetComponentsInChildren<Terrain>());
            ScanTerrainData();
        }

        private static readonly int[] ALPHAMAP_RESOLUTIONS =
        {
            16,
            32,
            64,
            128,
            256,
            512,
            1024,
            2048,
            4096
        };

        private static readonly int[] HEIGHTMAP_RESOLUTIONS =
        {
            33,
            65,
            129,
            257,
            513,
            1025,
            2049,
            4097
        };
        
        [UsedImplicitly]
        private bool AnyNullTerrains => terrains.Any(t => t == null);
        [UsedImplicitly]
        private bool NoTerrains => !terrains.Any();

        [UsedImplicitly]
        private IEnumerable<ValueDropdownItem<int>> GetHeightMapResolutions() =>
            HEIGHTMAP_RESOLUTIONS.Select(resolution => new ValueDropdownItem<int>($"{resolution} x {resolution}", resolution));

        [UsedImplicitly]
        private IEnumerable<ValueDropdownItem<int>> GetTextureResolutions() => 
            ALPHAMAP_RESOLUTIONS.Select(resolution => new ValueDropdownItem<int>($"{resolution} x {resolution}", resolution));
#endif
    }
}