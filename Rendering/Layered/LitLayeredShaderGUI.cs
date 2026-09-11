#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;

namespace EDIVE.Rendering.Layered
{
    // URP Lit inspector plus Top Layer.
    public class LitLayeredShaderGUI : BaseShaderGUI
    {
        private static readonly string[] WORKFLOW_MODE_NAMES = Enum.GetNames(typeof(LitGUI.WorkflowMode));
        private const uint TOP_LAYER_EXPANDABLE = 1u << 4;

        private static readonly GUIContent DETAIL_HEADER = EditorGUIUtility.TrTextContent("Detail Inputs");
        private static readonly GUIContent DETAIL_MASK = EditorGUIUtility.TrTextContent("Mask", "Alpha.");
        private static readonly GUIContent DETAIL_ALBEDO = EditorGUIUtility.TrTextContent("Base Map", "0.5 grey = no change.");

        private static readonly GUIContent TOP_HEADER = EditorGUIUtility.TrTextContent("Top Layer");
        private static readonly GUIContent TOP_MASK = EditorGUIUtility.TrTextContent("Mask", "Alpha 1 = top layer.");
        private static readonly GUIContent TOP_MASK_SHARPNESS = EditorGUIUtility.TrTextContent("Mask Sharpness", "Higher = harder edge.");
        private static readonly GUIContent TOP_MASK_NOISE = EditorGUIUtility.TrTextContent("Mask Noise", "Red breaks up the edge. 0.5 = no change.");
        private static readonly GUIContent TOP_MAP = EditorGUIUtility.TrTextContent("Base Map");
        private static readonly GUIContent TOP_METALLIC = EditorGUIUtility.TrTextContent("Metallic");
        private static readonly GUIContent TOP_SMOOTHNESS = EditorGUIUtility.TrTextContent("Smoothness");

        private const string DETAIL_MASK_PROP = "_DetailMask";
        private const string DETAIL_ALBEDO_MAP_PROP = "_DetailAlbedoMap";
        private const string DETAIL_ALBEDO_MAP_SCALE_PROP = "_DetailAlbedoMapScale";
        private const string DETAIL_NORMAL_MAP_PROP = "_DetailNormalMap";
        private const string DETAIL_NORMAL_MAP_SCALE_PROP = "_DetailNormalMapScale";
        private const string TOP_MASK_PROP = "_TopMask";
        private const string TOP_MASK_SHARPNESS_PROP = "_TopMaskSharpness";
        private const string TOP_MASK_NOISE_PROP = "_TopMaskNoise";
        private const string TOP_MASK_NOISE_MAP_PROP = "_TopMaskNoiseMap";
        private const string TOP_MAP_PROP = "_TopMap";
        private const string TOP_COLOR_PROP = "_TopColor";
        private const string TOP_METALLIC_PROP = "_TopMetallic";
        private const string TOP_SMOOTHNESS_PROP = "_TopSmoothness";
        private const string TOP_BUMP_MAP_PROP = "_TopBumpMap";
        private const string TOP_BUMP_SCALE_PROP = "_TopBumpScale";

        private const string DETAIL_MULX2_KEYWORD = "_DETAIL_MULX2";
        private const string DETAIL_SCALED_KEYWORD = "_DETAIL_SCALED";
        private const string NORMAL_MAP_KEYWORD = "_NORMALMAP";

        private static readonly int DETAIL_ALBEDO_MAP_ID = Shader.PropertyToID(DETAIL_ALBEDO_MAP_PROP);
        private static readonly int DETAIL_ALBEDO_MAP_SCALE_ID = Shader.PropertyToID(DETAIL_ALBEDO_MAP_SCALE_PROP);
        private static readonly int DETAIL_NORMAL_MAP_ID = Shader.PropertyToID(DETAIL_NORMAL_MAP_PROP);
        private static readonly int TOP_BUMP_MAP_ID = Shader.PropertyToID(TOP_BUMP_MAP_PROP);

        private LitGUI.LitProperties _litProperties;

        private MaterialProperty _detailMask;
        private MaterialProperty _detailAlbedoMap;
        private MaterialProperty _detailAlbedoMapScale;
        private MaterialProperty _detailNormalMap;
        private MaterialProperty _detailNormalMapScale;

        private MaterialProperty _topMask;
        private MaterialProperty _topMaskSharpness;
        private MaterialProperty _topMaskNoise;
        private MaterialProperty _topMaskNoiseMap;
        private MaterialProperty _topMap;
        private MaterialProperty _topColor;
        private MaterialProperty _topMetallic;
        private MaterialProperty _topSmoothness;
        private MaterialProperty _topBumpMap;
        private MaterialProperty _topBumpScale;

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            _litProperties = new LitGUI.LitProperties(properties);

            _detailMask = FindProperty(DETAIL_MASK_PROP, properties, false);
            _detailAlbedoMap = FindProperty(DETAIL_ALBEDO_MAP_PROP, properties, false);
            _detailAlbedoMapScale = FindProperty(DETAIL_ALBEDO_MAP_SCALE_PROP, properties, false);
            _detailNormalMap = FindProperty(DETAIL_NORMAL_MAP_PROP, properties, false);
            _detailNormalMapScale = FindProperty(DETAIL_NORMAL_MAP_SCALE_PROP, properties, false);

            _topMask = FindProperty(TOP_MASK_PROP, properties, false);
            _topMaskSharpness = FindProperty(TOP_MASK_SHARPNESS_PROP, properties, false);
            _topMaskNoise = FindProperty(TOP_MASK_NOISE_PROP, properties, false);
            _topMaskNoiseMap = FindProperty(TOP_MASK_NOISE_MAP_PROP, properties, false);
            _topMap = FindProperty(TOP_MAP_PROP, properties, false);
            _topColor = FindProperty(TOP_COLOR_PROP, properties, false);
            _topMetallic = FindProperty(TOP_METALLIC_PROP, properties, false);
            _topSmoothness = FindProperty(TOP_SMOOTHNESS_PROP, properties, false);
            _topBumpMap = FindProperty(TOP_BUMP_MAP_PROP, properties, false);
            _topBumpScale = FindProperty(TOP_BUMP_SCALE_PROP, properties, false);
        }

        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, SetLayerKeywords);
        }

        private static void SetLayerKeywords(Material material)
        {
            var scaled = !Mathf.Approximately(material.GetFloat(DETAIL_ALBEDO_MAP_SCALE_ID), 1f);
            var hasDetail = material.GetTexture(DETAIL_ALBEDO_MAP_ID) || material.GetTexture(DETAIL_NORMAL_MAP_ID);
            CoreUtils.SetKeyword(material, DETAIL_MULX2_KEYWORD, !scaled && hasDetail);
            CoreUtils.SetKeyword(material, DETAIL_SCALED_KEYWORD, scaled && hasDetail);

            if (material.GetTexture(TOP_BUMP_MAP_ID))
                CoreUtils.SetKeyword(material, NORMAL_MAP_KEYWORD, true);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            EditorGUIUtility.labelWidth = 0f;
            if (_litProperties.workflowMode != null)
                DoPopup(LitGUI.Styles.workflowModeText, _litProperties.workflowMode, WORKFLOW_MODE_NAMES);

            base.DrawSurfaceOptions(material);
        }

        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            LitGUI.Inputs(_litProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList)
        {
            materialScopesList.RegisterHeaderScope(DETAIL_HEADER, (uint) Expandable.Details, _ => DrawDetailInputs());
            materialScopesList.RegisterHeaderScope(TOP_HEADER, TOP_LAYER_EXPANDABLE, _ => DrawTopLayer());
        }

        public override void DrawAdvancedOptions(Material material)
        {
            Draw(_litProperties.highlights, LitGUI.Styles.highlightsText);
            Draw(_litProperties.reflections, LitGUI.Styles.reflectionsText);

            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            SetupMaterialBlendMode(material);
        }

        private void DrawDetailInputs()
        {
            materialEditor.TexturePropertySingleLine(DETAIL_MASK, _detailMask);
            materialEditor.TexturePropertySingleLine(DETAIL_ALBEDO, _detailAlbedoMap, _detailAlbedoMapScale);
            DrawNormalArea(materialEditor, _detailNormalMap, _detailNormalMapScale);
            if (_detailAlbedoMap != null)
                materialEditor.TextureScaleOffsetProperty(_detailAlbedoMap);
        }

        private void DrawTopLayer()
        {
            materialEditor.TexturePropertySingleLine(TOP_MASK, _topMask);
            Draw(_topMaskSharpness, TOP_MASK_SHARPNESS);
            materialEditor.TexturePropertySingleLine(TOP_MASK_NOISE, _topMaskNoiseMap, _topMaskNoise);
            if (_topMaskNoiseMap != null)
                materialEditor.TextureScaleOffsetProperty(_topMaskNoiseMap);

            EditorGUILayout.Space();
            materialEditor.TexturePropertySingleLine(TOP_MAP, _topMap, _topColor);
            Draw(_topMetallic, TOP_METALLIC);
            Draw(_topSmoothness, TOP_SMOOTHNESS);
            DrawNormalArea(materialEditor, _topBumpMap, _topBumpScale);
            if (_topMap != null)
                materialEditor.TextureScaleOffsetProperty(_topMap);
        }

        private void Draw(MaterialProperty property, GUIContent label)
        {
            if (property != null)
                materialEditor.ShaderProperty(property, label);
        }
    }
}
#endif
