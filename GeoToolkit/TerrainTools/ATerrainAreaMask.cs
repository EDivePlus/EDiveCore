// Author: František Holubec
// Created: 12.08.2026

using System;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using JetBrains.Annotations;
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace EDIVE.GeoToolkit.TerrainTools
{
    [RequireComponent(typeof(Terrain))]
    public abstract class ATerrainAreaMask<TLayer> : MonoBehaviour where TLayer : ATerrainAreaLayer
    {
        [SerializeField]
        [EnhancedValidate("ValidateMaskTexture")]
        private Texture2D _MaskTexture;

        [SerializeField]
        [EnhancedTableList(ShowFoldout = false, OnTitleBarGUI = "OnLayersTitleBarGUI")]
        private List<TLayer> _Layers = new();

        private Terrain _terrain;
        private Terrain Terrain => _terrain ??= GetComponent<Terrain>();

        public bool TrySampleLayer(Vector3 worldPosition, out TLayer resultLayer)
        {
            resultLayer = null;
            var terrainData = Terrain != null ? Terrain.terrainData : null;
            if (_MaskTexture == null || terrainData == null)
                return false;

            var local = worldPosition - Terrain.transform.position;
            var u = local.x / terrainData.size.x;
            var v = local.z / terrainData.size.z;
            if (u is < 0 or > 1 || v is < 0 or > 1)
                return false;

            var x = Mathf.Min((int) (u * _MaskTexture.width), _MaskTexture.width - 1);
            var y = Mathf.Min((int) (v * _MaskTexture.height), _MaskTexture.height - 1);
            var value = Mathf.RoundToInt(_MaskTexture.GetPixel(x, y).r * 255f);
            return TryGetLayer(value, out resultLayer);
        }

        private bool TryGetLayer(int value, out TLayer resultLayer)
        {
            return _Layers.TryGetFirst(l => l != null && l.Value == value, out resultLayer);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void OnLayersTitleBarGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(FontAwesomeEditorIcons.CopySolid))
                CopyLayersToOtherMasks();
        }

        private void CopyLayersToOtherMasks()
        {
            foreach (var mask in FindObjectsByType<ATerrainAreaMask<TLayer>>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mask == this)
                    continue;

                Undo.RecordObject(mask, "Copy Terrain Areas");
                mask._Layers = _Layers.ConvertAll(l => l == null ? null : JsonUtility.FromJson<TLayer>(JsonUtility.ToJson(l)));
                EditorUtility.SetDirty(mask);
            }
        }

        [UsedImplicitly]
        private void ValidateMaskTexture(Texture2D value, SelfValidationResult result)
        {
            if (value == null)
                return;

            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(value)) as TextureImporter;
            if (importer == null)
                return;

            if (importer.sRGBTexture || !importer.isReadable || importer.mipmapEnabled ||
                importer.filterMode != FilterMode.Point || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                result.AddError("Mask texture import settings are invalid").WithFix("Fix Import Settings", () =>
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.textureShape = TextureImporterShape.Texture2D;
                    importer.sRGBTexture = false;
                    importer.alphaSource = TextureImporterAlphaSource.None;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Point;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.isReadable = true;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.maxTextureSize = 8192;
                    importer.SaveAndReimport();
                });
            }
        }
#endif
    }

    [Serializable]
    public abstract class ATerrainAreaLayer
    {
        [SerializeField]
        [Tooltip("Red channel value of the mask pixel, 0-255.")]
        [EnhancedTableColumn(50)]
        private int _Value;
        
        public int Value => _Value;
    }
}
