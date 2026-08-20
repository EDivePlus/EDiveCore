// Author: František Holubec
// Created: 11.08.2026
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using EDIVE.GeoToolkit.Coordinates;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace EDIVE.GeoToolkit.Area
{
    public class GeoAreaBuilderWindow : OdinEditorWindow
    {
        private const int MAX_ITERATIONS = 24;
        private const double SIZE_TOLERANCE = 1e-6;
        
        [SerializeField]
        [EnumToggleButtons]
        private GeoAreaAnchor _AnchorType = GeoAreaAnchor.SouthWest;

        [SerializeField]
        [EnhancedInlineProperty]
        private GeoCoords _Anchor = new(double2.zero, CoordinateSystemType.EPSG_4326);
        
        [SerializeField]
        [SuffixLabel("m", true)]
        private double2 _GroundSize = new(4000, 4000);
        
        [SerializeField]
        private string _AssetName = "NewArea";

        [SerializeField]
        [FolderPath]
        private string _Folder = "Assets";

        [SerializeField]
        [ListDrawerSettings(DefaultExpandedState = true)]
        private List<CoordinateSystemType> _Systems = new()
        {
            CoordinateSystemType.EPSG_3857,
            CoordinateSystemType.EPSG_5514
        };

        [SerializeField]
        private bool _ReadOnlyAssets = true;

        [MenuItem("Tools/GeoToolkit/Area Builder")]
        private static void Open()
        {
            var window = GetWindow<GeoAreaBuilderWindow>();
            window.titleContent = new GUIContent("Area Builder");
            window.Show();
        }
        
        [ShowInInspector]
        [ListDrawerSettings(IsReadOnly = true, DefaultExpandedState = true, ShowFoldout = false)]
        private List<string> Preview => _Systems.Select(system => $"{_AssetName}-{system}").ToList();

        private GeoAreaRect BuildArea(CoordinateSystemType system) => FromAnchor(_Anchor, _AnchorType, _GroundSize, system);

        [PropertySpace]
        [Button]
        private void CreateAssets()
        {
            if (string.IsNullOrWhiteSpace(_AssetName) || _Systems.Count == 0)
            {
                Debug.LogError($"[{nameof(GeoAreaBuilderWindow)}] Asset name and at least one coordinate system are required.");
                return;
            }

            foreach (var system in _Systems)
                Write(_Folder, _AssetName, BuildArea(system), _ReadOnlyAssets);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [Button("Show Area In Map")]
        private void ShowAreaInMap() => GeoJsonPreviewUtility.OpenAreas(_Systems.Select(BuildArea));

        private static void Write(string folder, string assetName, GeoAreaRect area, bool readOnly = true)
        {
            PathUtility.EnsureAssetsPathExists(folder);
            var path = $"{folder}/{assetName}-{area.CoordinateSystem}.asset";

            var asset = AssetDatabase.LoadAssetAtPath<GeoAreaRectScriptableVariable>(path);
            if (asset == null)
            {
                asset = CreateInstance<GeoAreaRectScriptableVariable>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.SetReadOnly(readOnly);
            var value = new GeoAreaRect(area.Min, area.Max, area.CoordinateSystem, area.GeoSize);
            asset.SetDefaultValue(value);

            EditorUtility.SetDirty(asset);
            Debug.Log($"[GeoAreaBuilder] {path}\n" +
                      $"Min {area.Min.x:F6}, {area.Min.y:F6}\n" +
                      $"Max {area.Max.x:F6}, {area.Max.y:F6}\n" +
                      $"Ground {area.GeoSize.x:F6} x {area.GeoSize.y:F6} m", asset);
        }
        
        private static GeoAreaRect FromAnchor(GeoCoords anchor, GeoAreaAnchor anchorType, double2 groundSize, CoordinateSystemType targetSystem)
        {
            var anchorPosition = anchor.ConvertTo(targetSystem).Position;
            var pivot = anchorType.GetPivot();
            var projectedSize = groundSize;

            for (var i = 0; i < MAX_ITERATIONS; i++)
            {
                var measured = Build(anchorPosition, pivot, projectedSize, targetSystem).CalculateGeoSize();
                if (measured.x <= 0 || measured.y <= 0)
                    break;

                if (math.all(math.abs(measured - groundSize) < SIZE_TOLERANCE))
                    break;

                projectedSize *= groundSize / measured;
            }

            var result = Build(anchorPosition, pivot, projectedSize, targetSystem);
            return new GeoAreaRect(result.Min, result.Max, targetSystem, groundSize);
        }

        private static GeoAreaRect Build(double2 anchorPosition, double2 pivot, double2 projectedSize, CoordinateSystemType targetSystem)
        {
            var min = anchorPosition - pivot * projectedSize;
            return new GeoAreaRect(min, min + projectedSize, targetSystem);
        }
    }
}

#endif