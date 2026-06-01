#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.DataStructures;
using EDIVE.DataStructures.VariableFields;
using EDIVE.GeoToolkit.Area;
using EDIVE.GeoToolkit.Maps;
using EDIVE.GeoToolkit.WebMapServices;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.GeoToolkit.TerrainTools
{
    public class TerrainGenerator : MonoBehaviour
    {
        [SerializeField]
        [PropertyOrder(-10)]
        [Required]
        private WebMapServiceRequestDefinition heightMapRasterData;

        [SerializeField]
        [PropertyOrder(-10)]
        [Required]
        private WebMapServiceRequestDefinition orthoPhotoRasterData;
        
        [PropertySpace]
        [PropertyOrder(-10)]
        [SerializeField]
        protected VariableField<GeoAreaRect> _Area = new();

        [SerializeField]
        [PropertyOrder(-10)]
        [LayerField]
        private int terrainLayer;
        
        [SerializeField]
        [PropertyOrder(-10)]
        private float2 resultTerrainSize;
        
        [SerializeField]
        [PropertyOrder(-10)]
        private float terrainYScale = 1f;
        
        [ShowInInspector]
        [PropertyOrder(-10)]
        private float2 AreaSizeMultiplier => resultTerrainSize / (float2) (_Area?.Value.GeoSize ?? double2.zero);

        [ShowInInspector]
        [PropertyOrder(-10)]
        [SuffixLabel("%", true)]
        private double XYDeformationDiffPercentage => (1 - (1 / AreaSizeMultiplier.x * AreaSizeMultiplier.y)) * 100;
        
        [PropertySpace]
        [SerializeField]
        [EnumToggleButtons]
        private TerrainType terrainType = TerrainType.UnityTerrain;
        
        [SerializeField]
        private bool drawHoles;
        
        [PropertySpace]
        [SerializeField]
        private int2 tileGridSize;

        [SerializeField]
        [InfoBox("$GetOrthoTextureSizeInfo", InfoMessageType.None, VisibleIf = nameof(AreaAssigned))]
        private int2 orthoTextureSize;

        [SerializeField]
        [InfoBox("$GetHeightTextureSizeInfo", InfoMessageType.None, VisibleIf = nameof(AreaAssigned))]
        private int2 heightMapTextureSize;

        [SerializeField]
        [Required]
        private Transform terrainRoot;

        [SerializeField]
        [Required]
        private Material materialTemplate;

        [InfoBox("Non-existing folders will be created!", InfoMessageType.None)]
        [PropertySpace]
        [SerializeField]
        [FolderPath]
        private string terrainAssetsFolderRoot;

        [FormerlySerializedAs("orthoPhotoFolder")]
        [SerializeField]
        [FolderPath]
        private string orthoPhotoFolderOverride;

        [FormerlySerializedAs("terrainFolder")]
        [SerializeField]
        [FolderPath]
        private string terrainDataFolderOverride;

        [FormerlySerializedAs("materialFolder")]
        [SerializeField]
        [FolderPath]
        private string materialFolderOverride;
        
        private string OrthoPhotoFolder => string.IsNullOrWhiteSpace(orthoPhotoFolderOverride) ? $"{terrainAssetsFolderRoot}/Textures" : orthoPhotoFolderOverride;
        private string TerrainDataFolder => string.IsNullOrWhiteSpace(terrainDataFolderOverride) ? $"{terrainAssetsFolderRoot}/Terrains" : terrainDataFolderOverride;
        private string MaterialFolder => string.IsNullOrWhiteSpace(materialFolderOverride) ? $"{terrainAssetsFolderRoot}/Materials" : materialFolderOverride;
        
        private CancellationTokenSource downloadCts;

        private Serializable2DArray<GeoAreaRect> areaBBoxGrid;
        private float2 tileSize;

        private float terrainMin;
        private float terrainMax;

        [EnhancedBoxGroup("Generator")] [SerializeField]
        private bool downloadOrthoPhoto = true;

        [EnhancedBoxGroup("Generator")] [SerializeField]
        private bool generateMaterials = true;

        [EnhancedBoxGroup("Generator", Color = "@Color.yellow", SpaceBefore = 8)] [SerializeField]
        private bool downloadHeightMaps = true;

        [EnhancedBoxGroup("Generator")] [SerializeField]
        private bool generateTerrains = true;
        
        private static readonly int TERRAIN_HOLES_TEXTURE = Shader.PropertyToID("_TerrainHolesTexture");
        private static readonly int HOLE_VALUE = Shader.PropertyToID("_HoleValue");

        private const int DownloadOrthoPhotoWeight = 8;
        private const int GenerateMaterialsWeight = 1;
        private const int DownloadHeightMapsWeight = 5;
        private const int GenerateTerrainsWeight = 1;

        private HeightMapAsset HeightMapAsset
        {
            get
            {
                var heightMapAssetPath = $"{TerrainDataFolder}/HeightMap.asset";

                var heightMapAsset = (HeightMapAsset) AssetDatabase.LoadAssetAtPath(heightMapAssetPath, typeof(HeightMapAsset));
                if (heightMapAsset == null)
                {
                    heightMapAsset = ScriptableObject.CreateInstance<HeightMapAsset>();
                    AssetDatabase.CreateAsset(heightMapAsset, heightMapAssetPath);
                }

                return heightMapAsset;
            }
        }

        [EnhancedBoxGroup("Generator")]
        [Button("Generate")]
        public void GenerateTerrain()
        {
            downloadCts?.Cancel();
            downloadCts?.Dispose();
            downloadCts = new CancellationTokenSource();
            GenerateTerrainAsync(downloadCts.Token).Forget();
        }

        private async UniTaskVoid GenerateTerrainAsync(CancellationToken cancellationToken)
        {
            try
            {
                PathUtility.EnsureAssetsPathExists(TerrainDataFolder);
                PathUtility.EnsureAssetsPathExists(OrthoPhotoFolder);
                PathUtility.EnsureAssetsPathExists(MaterialFolder);
                PathUtility.EnsureAssetsPathExists(TerrainDataFolder);

                tileSize = resultTerrainSize / tileGridSize;
                areaBBoxGrid = new Serializable2DArray<GeoAreaRect>(_Area.Value.Split(tileGridSize));

                var heightMapAsset = HeightMapAsset;
                if (downloadHeightMaps)
                {
                    heightMapAsset.Data = new Serializable2DArray<Serializable2DArray<float>>(new Vector2Int(tileGridSize.x, tileGridSize.y));
                }

                if (generateTerrains)
                {
                    if (PrefabUtility.IsOutermostPrefabInstanceRoot(terrainRoot.gameObject))
                        PrefabUtility.UnpackPrefabInstance(terrainRoot.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);

                    terrainRoot.DestroyChildrenImmediate();

                    terrainMin = float.MaxValue;
                    terrainMax = float.MinValue;

                    if (!downloadHeightMaps)
                    {
                        if (heightMapAsset.Data.Size != new Vector2Int(tileGridSize.x, tileGridSize.y))
                        {
                            Debug.LogError("Current height map data are invalid, download height map data to proceed to generating terrain!");
                            return;
                        }

                        var heightMaps = heightMapAsset.Data.GetArray();
                        foreach (var heightMap in heightMaps)
                        {
                            var heightMapFlat = heightMap.GetArray();
                            terrainMax = math.max(terrainMax, heightMapFlat.Max());
                            terrainMin = math.min(terrainMin, heightMapFlat.Min());
                        }
                    }
                }

                var weights = new float[]
                {
                    downloadOrthoPhoto ? DownloadOrthoPhotoWeight : 0,
                    generateMaterials ? GenerateMaterialsWeight : 0,
                    downloadHeightMaps ? DownloadHeightMapsWeight : 0,
                    generateTerrains ? GenerateTerrainsWeight : 0,
                };
                var progressRangeEstimates = Vector2.up.SplitWeighted(weights);

                if (downloadOrthoPhoto) await DownloadOrthoPhoto(progressRangeEstimates[0], cancellationToken);
                if (generateMaterials) await GenerateMaterials(progressRangeEstimates[1], cancellationToken);
                if (downloadHeightMaps) await DownloadHeightMaps(progressRangeEstimates[2], cancellationToken);
                if (generateTerrains)
                {
                    switch (terrainType)
                    {
                        case TerrainType.UnityTerrain:
                            await GenerateUnityTerrains(progressRangeEstimates[3], cancellationToken);
                            break;
                        case TerrainType.Mesh:
                            await GenerateMeshTerrains(progressRangeEstimates[3], cancellationToken);
                            break;
                    }
                }

                FinishCreateProcess();
            }
            catch (OperationCanceledException)
            {
                EditorUtility.ClearProgressBar();
            }
            finally
            {
                downloadCts?.Dispose();
                downloadCts = null;
            }
        }

        private async UniTask DownloadOrthoPhoto(float2? progressRange = null, CancellationToken cancellationToken = default)
        {
            progressRange ??= new float2(0, 1);
            var progressPart = 1f / (tileGridSize.x * tileGridSize.y);

            for (var x = 0; x < tileGridSize.x; x++)
            {
                for (var y = 0; y < tileGridSize.y; y++)
                {
                    var localProgressBase = (x * tileGridSize.y + y) * progressPart;

                    var areaPart = areaBBoxGrid[x, y];
                    var currX = x;
                    var currY = y;

                    var absoluteOrthoPhotoFolder = PathUtility.GetAbsolutePath(OrthoPhotoFolder);
                    var orthoPhotoFileName = $"OrthoPhoto{x}{y}.png";

                    var progress = Cysharp.Threading.Tasks.Progress.Create<float>(p =>
                    {
                        var globalProgress = (localProgressBase + progressPart * p).Remap(0, 1, progressRange.Value.x, progressRange.Value.y);
                        if (EditorUtility.DisplayCancelableProgressBar("Generating", $"Downloading OrthoPhoto{currX}{currY} ({p * 100:0.#}%)", globalProgress))
                            downloadCts?.Cancel();
                    });

                    var orthoResult = await orthoPhotoRasterData.Request.DownloadAsync(areaPart, orthoTextureSize, progress, cancellationToken);
                    var orthoTexture = orthoResult.GetTexture();

                    var globalFilePath = $"{absoluteOrthoPhotoFolder}/{orthoPhotoFileName}";
                    var localFilePath = $"{OrthoPhotoFolder}/{orthoPhotoFileName}";
                    await File.WriteAllBytesAsync(globalFilePath, orthoTexture.EncodeToPNG(), cancellationToken);
                    AssetDatabase.ImportAsset(localFilePath);
                    var textureImporter = AssetImporter.GetAtPath(localFilePath) as TextureImporter;
                    if (textureImporter != null)
                    {
                        textureImporter.isReadable = true;
                        textureImporter.wrapMode = TextureWrapMode.Mirror;
                    }
                    AssetDatabase.ImportAsset(localFilePath);
                    AssetDatabase.Refresh();

                    await UniTask.Yield(cancellationToken);
                }
            }
        }

        private async UniTask GenerateMaterials(float2? progressRange = null, CancellationToken cancellationToken = default)
        {
            progressRange ??= new float2(0, 1);
            var progressPart = 1f / (tileGridSize.x * tileGridSize.y);

            for (var x = 0; x < tileGridSize.x; x++)
            {
                for (var y = 0; y < tileGridSize.y; y++)
                {
                    var localProgress = (x * tileGridSize.y + y) * progressPart;
                    var progress = localProgress.Remap(0, 1, progressRange.Value.x, progressRange.Value.y);
                    if (EditorUtility.DisplayCancelableProgressBar("Generating", $"Processing OrthoPhoto{x}{y}", progress))
                    {
                        throw new OperationCanceledException();
                    }

                    var orthoPhotoFileName = $"OrthoPhoto{x}{y}.png";
                    var orthoPhotoRelativePath = $"{OrthoPhotoFolder}/{orthoPhotoFileName}";

                    var texture = (Texture) AssetDatabase.LoadAssetAtPath(orthoPhotoRelativePath, typeof(Texture2D));
                    var material = new Material(materialTemplate)
                    {
                        mainTexture = texture
                    };

                    var matAssetPath = $"{MaterialFolder}/OrthoPhotoMat{x}{y}.mat";
                    AssetDatabase.CreateAsset(material, matAssetPath);
                    await UniTask.Yield(cancellationToken);
                }
            }
        }

        private async UniTask DownloadHeightMaps(float2? progressRange = null, CancellationToken cancellationToken = default)
        {
            progressRange ??= new float2(0, 1);
            var progressPart = 1f / (tileGridSize.x * tileGridSize.y);

            var heightMapAsset = HeightMapAsset;
            for (var x = 0; x < tileGridSize.x; x++)
            {
                for (var y = 0; y < tileGridSize.y; y++)
                {
                    var areaPart = areaBBoxGrid[x, y];
                    var currX = x;
                    var currY = y;
                    var localProgressBase = (x * tileGridSize.y + y) * progressPart;
                    
                    var progress = Cysharp.Threading.Tasks.Progress.Create<float>(p =>
                    {
                        var globalProgress = (localProgressBase + progressPart * p).Remap(0, 1, progressRange.Value.x, progressRange.Value.y);
                        if (EditorUtility.DisplayCancelableProgressBar("Generating", $"Downloading HeightMap{currX}{currY} ({p * 100:0.#}%)", globalProgress))
                            downloadCts?.Cancel();
                    });

                    var data = await heightMapRasterData.Request.DownloadAsync(areaPart, heightMapTextureSize, progress, cancellationToken);
                    var heightMap = data.GetGrayScale2DArray()
                        .ToFloat()
                        .Apply((texture, i, j) => texture[i, j].Approximately(255) ? 0 : texture[i, j]);
                   
                    if (heightMap == null)
                    {
                        Debug.LogError("HeightMap data load failed!");
                        continue;
                    }

                    heightMapAsset.Data[x, y] = new Serializable2DArray<float>(heightMap);
                    await UniTask.Yield(cancellationToken);
                }
            }

            EditorUtility.SetDirty(heightMapAsset);
            AssetDatabase.SaveAssets();
        }
        
        private async UniTask GenerateMeshTerrains(float2? progressRange = null, CancellationToken cancellationToken = default)
        {
            progressRange ??= new float2(0, 1);
            var progressPart = 1f / (tileGridSize.x * tileGridSize.y);

            var heightMapAsset = HeightMapAsset;
            heightMapAsset.FixTerrainHeightGaps();

            for (var x = 0; x < tileGridSize.x; x++)
            {
                for (var y = 0; y < tileGridSize.y; y++)
                {
                    var heightMapFlat = heightMapAsset.Data[x, y].GetArray();
                    var nonZeroValues = heightMapFlat.Where(i => i != 0);
                    terrainMax = math.max(terrainMax, nonZeroValues.Max());
                    terrainMin = math.min(terrainMin, nonZeroValues.Min());
                }
            }

            await UniTask.Yield(cancellationToken);

            for (var x = 0; x < tileGridSize.x; x++)
            {
                for (var y = 0; y < tileGridSize.y; y++)
                {
                    var localProgress = (x * tileGridSize.y + y) * progressPart;
                    var progress = localProgress.Remap(0, 1, progressRange.Value.x, progressRange.Value.y);
                    if (EditorUtility.DisplayCancelableProgressBar("Generating", $"Processing downloaded data into Terrain [{x},{y}]", progress))
                    {
                        throw new OperationCanceledException();
                    }

                    var terrain = new GameObject($"Terrain[{x};{y}]");
                    terrain.gameObject.layer = terrainLayer;
                    var terrainTr = terrain.transform;
                    terrainTr.SetParent(terrainRoot);
                    terrainTr.Reset();
                    terrainTr.localPosition = new Vector3(tileSize.x * x, 0, tileSize.y * y);
                    
                    var meshFilter = terrain.AddComponent<MeshFilter>();
                    var meshRenderer = terrain.AddComponent<MeshRenderer>();
                    var meshCollider = terrain.AddComponent<MeshCollider>();
                    
                    var areaYMultiplier = math.max(AreaSizeMultiplier.x, AreaSizeMultiplier.y);
                    var terrainHeight = areaYMultiplier * (terrainMax - terrainMin) * terrainYScale;
                    var heightMap = heightMapAsset.Data[x, y]?.Get2DArray() ?? new float[0, 0];

                    heightMap.Remap(terrainMin, terrainMax, 0, 1);
                    heightMap.Clamp(0, 1);
                    if (heightMap.GetLength(0) == 0 || heightMap.GetLength(1) == 0)
                    {
                        Debug.LogError($"Invalid Terrain tile [{x};{y}]");
                        continue;
                    }
                    
                    var mesh = new Mesh();
                    var vertices = new List<Vector3>();
                    var triangles = new List<int>();
                    var uvs = new List<Vector2>();

                    var heightMapWidth = heightMap.GetLength(0);
                    var heightMapHeight = heightMap.GetLength(1);
                    
                    var xValueSize = tileSize.x / (heightMapWidth - 1);
                    var yValueSize = tileSize.y / (heightMapHeight - 1);
                    for (var pX = 0; pX < heightMapWidth; pX++)
                    {
                        for (var pY = 0; pY < heightMapHeight; pY++)
                        {
                            var value = heightMap[pY, pX];
                            var height = terrainHeight * value;
                            vertices.Add(new Vector3(pX * xValueSize, height, pY * yValueSize));
                            uvs.Add(new Vector2(1f / (heightMapWidth-1) * pX, 1f / (heightMapHeight-1) * pY));
                        }
                    }
                    
                    for (var pX = 0; pX < heightMapWidth - 1; pX++)
                    {
                        for (var pY = 0; pY < heightMapHeight - 1; pY++)
                        {
                            var a = pY + pX * heightMapHeight;
                            var b = pY + (pX + 1) * heightMapHeight;
                            var c = pY + 1 + (pX + 1) * heightMapHeight;
                            var d = pY + 1 + pX * heightMapHeight;

                            if ((vertices[a].y + vertices[b].y + vertices[c].y + vertices[d].y).Approximately(0))
                                continue;

                            triangles.AddRange(new[] {c, b, a});
                            triangles.AddRange(new[] {d, c, a});
                        }
                    }
                    
                    mesh.vertices = vertices.ToArray();
                    mesh.triangles = triangles.ToArray();
                    mesh.uv = uvs.ToArray();
                    mesh.RecalculateNormals();
                    mesh.Optimize();
                    
                    meshFilter.sharedMesh = mesh;
                    meshCollider.sharedMesh = mesh;
                    
                    var matAssetPath = $"{MaterialFolder}/OrthoPhotoMat{x}{y}.mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(matAssetPath);
                    material.SetTexture(TERRAIN_HOLES_TEXTURE, material.mainTexture);
                    material.SetFloat(HOLE_VALUE, 1f);
                    meshRenderer.material = material;
                    await UniTask.Yield(cancellationToken);
                }
            }

            var meshSource = new MeshMapSource();
            meshSource.PopulateFromChildren(terrainRoot);
            var mapController = terrainRoot.GetOrAddComponent<MapController>();
            mapController.Initialize(meshSource, _Area.Value);

            terrainRoot.DestroyComponentImmediate<MultiTerrainController>();
        }

        private async UniTask GenerateUnityTerrains(float2? progressRange = null, CancellationToken cancellationToken = default)
        {
            progressRange ??= new float2(0, 1);
            var progressPart = 1f / (tileGridSize.x * tileGridSize.y);

            var heightMapAsset = HeightMapAsset;
            heightMapAsset.FixTerrainHeightGaps();

            for (var x = 0; x < tileGridSize.x; x++)
            {
                for (var y = 0; y < tileGridSize.y; y++)
                {
                    var heightMapFlat = heightMapAsset.Data[x, y].GetArray();
                    var nonZeroValues = heightMapFlat.Where(i => i != 0);
                    terrainMax = math.max(terrainMax, nonZeroValues.Max());
                    terrainMin = math.min(terrainMin, nonZeroValues.Min());
                }
            }

            await UniTask.Yield(cancellationToken);

            var terrainGrid = new Serializable2DArray<Terrain>(tileGridSize.x, tileGridSize.y);
            for (var x = 0; x < tileGridSize.x; x++)
            {
                for (var y = 0; y < tileGridSize.y; y++)
                {
                    var localProgress = (x * tileGridSize.y + y) * progressPart;
                    var progress = localProgress.Remap(0, 1, progressRange.Value.x, progressRange.Value.y);
                    if (EditorUtility.DisplayCancelableProgressBar("Generating", $"Processing downloaded data into Terrain [{x},{y}]", progress))
                    {
                        throw new OperationCanceledException();
                    }

                    var areaYMultiplier = math.max(AreaSizeMultiplier.x, AreaSizeMultiplier.y);
                    var terrainHeight = areaYMultiplier * (terrainMax - terrainMin) * terrainYScale;
                    var heightmapResolution = math.max(heightMapTextureSize.x, heightMapTextureSize.y);
                    var terrainData = new TerrainData
                    {
                        heightmapResolution = heightmapResolution,
                        size = new Vector3(tileSize.x, terrainHeight, tileSize.y)
                    };

                    var heightMap = heightMapAsset.Data[x, y]?.Get2DArray() ?? new float[0, 0];

                    if (drawHoles)
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
                    if (heightMap.GetLength(0) == 0 || heightMap.GetLength(1) == 0)
                    {
                        Debug.LogError($"Invalid Terrain tile [{x};{y}]");
                        continue;
                    }
                    
                    terrainData.SetHeights(0, 0, heightMap);

                    var terrainDataPath = $"{TerrainDataFolder}/TerrainData{x}{y}.asset";
                    AssetDatabase.CreateAsset(terrainData, terrainDataPath);
                    terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(terrainDataPath);

                    var terrain = Terrain.CreateTerrainGameObject(terrainData).GetComponent<Terrain>();
                    terrain.gameObject.layer = terrainLayer;
                    terrain.gameObject.name = $"Terrain[{x};{y}]";
                    var terrainTr = terrain.transform;
                    terrainTr.SetParent(terrainRoot);
                    terrainTr.Reset();
                    terrainTr.localPosition = new Vector3(tileSize.x * x, 0, tileSize.y * y);

                    terrainGrid[x, y] = terrain;

                    var matAssetPath = $"{MaterialFolder}/OrthoPhotoMat{x}{y}.mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(matAssetPath);
                    material.SetTexture(TERRAIN_HOLES_TEXTURE, material.mainTexture);
                    material.SetFloat(HOLE_VALUE, 1f);
                    terrain.materialTemplate = material;

                    await UniTask.Yield(cancellationToken);
                }
            }
            
            for (var x = 0; x < tileGridSize.x; x++)
            {
                for (var y = 0; y < tileGridSize.y; y++)
                {
                    if (terrainGrid[x, y] == null) continue;

                    var right = GetTerrain(x + 1, y);
                    var top = GetTerrain(x, y + 1);
                    var left = GetTerrain(x - 1, y);
                    var bottom = GetTerrain(x, y - 1);

                    terrainGrid[x, y].SetNeighbors(left, top, right, bottom);

                    if (right != null)
                    {
                        var heights = right.terrainData.GetHeights(0, 0, 1, heightMapTextureSize.x);
                        terrainGrid[x, y].terrainData.SetHeights(heightMapTextureSize.x - 1, 0, heights);
                    }

                    if (top != null)
                    {
                        var heights = top.terrainData.GetHeights(0, 0, heightMapTextureSize.y, 1);
                        terrainGrid[x, y].terrainData.SetHeights(0, heightMapTextureSize.y - 1, heights);
                    }
                }
            }

            Terrain GetTerrain(int x, int y)
            {
                if (x < 0 || x >= tileGridSize.x || y < 0 || y >= tileGridSize.y)
                {
                    return null;
                }

                return terrainGrid[x, y];
            }

            var terrainSource = new TerrainMapSource();
            terrainSource.PopulateFromChildren(terrainRoot);
            var mapController = terrainRoot.GetOrAddComponent<MapController>();
            mapController.Initialize(terrainSource, _Area.Value);

            var multiTerrainEditor = terrainRoot.GetOrAddComponent<MultiTerrainController>();
            multiTerrainEditor.RefreshTerrains();
        }

        private void FinishCreateProcess()
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.SetDirty(HeightMapAsset);
            AssetDatabase.SaveAssets();
        }
        
        [PropertySpace]
        [Button]
        public void ClearFailedProgress()
        {
            downloadCts?.Cancel();
            downloadCts?.Dispose();
            downloadCts = null;
            EditorUtility.ClearProgressBar();
        }

        [UsedImplicitly]
        private string GetOrthoTextureSizeInfo()
        {
            if (_Area == null)
            {
                return "Area not specified";
            }
            var perMeterSize = (float2) _Area.Value.GeoSize / tileGridSize / orthoTextureSize;
            return $"Result ortho texture pixel represents approximately {perMeterSize.x:0.000}m x {perMeterSize.y:0.000}m of the real area";
        }
        
        [UsedImplicitly]
        private string GetHeightTextureSizeInfo()
        {
            if (_Area == null)
            {
                return "Area not specified";
            }
            var perMeterSize = (float2) _Area.Value.GeoSize / tileGridSize / heightMapTextureSize;
            return $"Result height map texture pixel represents approximately {perMeterSize.x:0.000}m x {perMeterSize.y:0.000}m of the real area";
        }

        [UsedImplicitly]
        private bool AreaAssigned => _Area != null;
    }
}
#endif