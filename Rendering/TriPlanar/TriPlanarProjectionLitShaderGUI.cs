#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;

namespace EDIVE.Rendering.TriPlanar
{
    // URP Lit inspector with triplanar projection instead of UV tiling, plus a Detail Inputs foldout.
    public class TriPlanarProjectionLitShaderGUI : BaseShaderGUI
    {
        private static readonly GUIContent BASE_MAP_STRENGTH = EditorGUIUtility.TrTextContent("Base Map Strength", "0 = color only.");
        private static readonly GUIContent PROJECTION_HEADER = EditorGUIUtility.TrTextContent("Triplanar Projection");
        private static readonly GUIContent SPACE = EditorGUIUtility.TrTextContent("Space", "World space, or local space so the texture follows the object.");
        private static readonly GUIContent TILING = EditorGUIUtility.TrTextContent("Tiling", "Repeats per unit, per axis.");
        private static readonly GUIContent OFFSET = EditorGUIUtility.TrTextContent("Offset", "Shifts the projection origin.");
        private static readonly GUIContent BLEND_SHARPNESS = EditorGUIUtility.TrTextContent("Blend Sharpness", "Higher is sharper.");

        private static readonly GUIContent DETAIL_HEADER = EditorGUIUtility.TrTextContent("Detail Inputs", "Overlaid at its own tiling.");
        private static readonly GUIContent DETAIL_MASK = EditorGUIUtility.TrTextContent("Mask", "Alpha masks the detail. Uses the base tiling.");
        private static readonly GUIContent DETAIL_ALBEDO = EditorGUIUtility.TrTextContent("Base Map", "Multiplied over the base. 0.5 grey leaves it unchanged.");
        private static readonly GUIContent DETAIL_TILING = EditorGUIUtility.TrTextContent("Tiling", "Repeats per unit. Avoid whole multiples of the base tiling.");
        private static readonly GUIContent DETAIL_OFFSET = EditorGUIUtility.TrTextContent("Offset", "Shifts the detail layer.");

        private const string BASE_MAP_STRENGTH_PROP = "_BaseMapStrength";
        private const string PROJECTION_SPACE_PROP = "_ProjectionSpace";
        private const string TILING_PROP = "_Tiling";
        private const string PROJECTION_OFFSET_PROP = "_ProjectionOffset";
        private const string BLEND_SHARPNESS_PROP = "_BlendSharpness";
        private const string DETAIL_MASK_PROP = "_DetailMask";
        private const string DETAIL_ALBEDO_MAP_PROP = "_DetailAlbedoMap";
        private const string DETAIL_ALBEDO_MAP_SCALE_PROP = "_DetailAlbedoMapScale";
        private const string DETAIL_NORMAL_MAP_PROP = "_DetailNormalMap";
        private const string DETAIL_NORMAL_MAP_SCALE_PROP = "_DetailNormalMapScale";
        private const string DETAIL_TILING_PROP = "_DetailTiling";
        private const string DETAIL_OFFSET_PROP = "_DetailOffset";

        private const string DETAIL_MULX2_KEYWORD = "_DETAIL_MULX2";
        private const string DETAIL_SCALED_KEYWORD = "_DETAIL_SCALED";
        
        private static readonly int DETAIL_ALBEDO_MAP_ID = Shader.PropertyToID(DETAIL_ALBEDO_MAP_PROP);
        private static readonly int DETAIL_ALBEDO_MAP_SCALE_ID = Shader.PropertyToID(DETAIL_ALBEDO_MAP_SCALE_PROP);
        private static readonly int DETAIL_NORMAL_MAP_ID = Shader.PropertyToID(DETAIL_NORMAL_MAP_PROP);

        private LitGUI.LitProperties _litProperties;

        private MaterialProperty _baseMapStrength;
        private MaterialProperty _projectionSpace;
        private MaterialProperty _tiling;
        private MaterialProperty _projectionOffset;
        private MaterialProperty _blendSharpness;

        private MaterialProperty _detailMask;
        private MaterialProperty _detailAlbedoMap;
        private MaterialProperty _detailAlbedoMapScale;
        private MaterialProperty _detailNormalMap;
        private MaterialProperty _detailNormalMapScale;
        private MaterialProperty _detailTiling;
        private MaterialProperty _detailOffset;

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            _litProperties = new LitGUI.LitProperties(properties);

            _baseMapStrength = FindProperty(BASE_MAP_STRENGTH_PROP, properties, false);
            _projectionSpace = FindProperty(PROJECTION_SPACE_PROP, properties, false);
            _tiling = FindProperty(TILING_PROP, properties, false);
            _projectionOffset = FindProperty(PROJECTION_OFFSET_PROP, properties, false);
            _blendSharpness = FindProperty(BLEND_SHARPNESS_PROP, properties, false);

            _detailMask = FindProperty(DETAIL_MASK_PROP, properties, false);
            _detailAlbedoMap = FindProperty(DETAIL_ALBEDO_MAP_PROP, properties, false);
            _detailAlbedoMapScale = FindProperty(DETAIL_ALBEDO_MAP_SCALE_PROP, properties, false);
            _detailNormalMap = FindProperty(DETAIL_NORMAL_MAP_PROP, properties, false);
            _detailNormalMapScale = FindProperty(DETAIL_NORMAL_MAP_SCALE_PROP, properties, false);
            _detailTiling = FindProperty(DETAIL_TILING_PROP, properties, false);
            _detailOffset = FindProperty(DETAIL_OFFSET_PROP, properties, false);
        }

        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, SetDetailKeywords);
        }

        private static void SetDetailKeywords(Material material)
        {
            var scaled = !Mathf.Approximately(material.GetFloat(DETAIL_ALBEDO_MAP_SCALE_ID), 1f);
            var hasDetail = material.GetTexture(DETAIL_ALBEDO_MAP_ID) || material.GetTexture(DETAIL_NORMAL_MAP_ID);
            CoreUtils.SetKeyword(material, DETAIL_MULX2_KEYWORD, !scaled && hasDetail);
            CoreUtils.SetKeyword(material, DETAIL_SCALED_KEYWORD, scaled && hasDetail);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            EditorGUIUtility.labelWidth = 0f;
            if (_litProperties.workflowMode != null)
                DoPopup(LitGUI.Styles.workflowModeText, _litProperties.workflowMode, System.Enum.GetNames(typeof(LitGUI.WorkflowMode)));

            base.DrawSurfaceOptions(material);
        }

        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            LitGUI.Inputs(_litProperties, materialEditor, material);
            Draw(_baseMapStrength, BASE_MAP_STRENGTH);
            DrawEmissionProperties(material, true);
            DrawProjection();
        }

        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList)
        {
            materialScopesList.RegisterHeaderScope(DETAIL_HEADER, (uint) Expandable.Details, _ => DrawDetailInputs());
        }

        public override void DrawAdvancedOptions(Material material)
        {
            Draw(_litProperties.highlights, LitGUI.Styles.highlightsText);
            Draw(_litProperties.reflections, LitGUI.Styles.reflectionsText);

            base.DrawAdvancedOptions(material);
        }

        private void DrawProjection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(PROJECTION_HEADER, EditorStyles.boldLabel);

            Draw(_projectionSpace, SPACE);
            DrawVector3(_tiling, TILING);
            DrawVector3(_projectionOffset, OFFSET);
            Draw(_blendSharpness, BLEND_SHARPNESS);
        }

        private void DrawDetailInputs()
        {
            materialEditor.TexturePropertySingleLine(DETAIL_MASK, _detailMask);
            materialEditor.TexturePropertySingleLine(DETAIL_ALBEDO, _detailAlbedoMap, _detailAlbedoMapScale);
            DrawNormalArea(materialEditor, _detailNormalMap, _detailNormalMapScale);
            DrawVector3(_detailTiling, DETAIL_TILING);
            DrawVector3(_detailOffset, DETAIL_OFFSET);
        }

        private void Draw(MaterialProperty property, GUIContent label)
        {
            if (property != null)
                materialEditor.ShaderProperty(property, label);
        }
        
        private void DrawVector3(MaterialProperty property, GUIContent label)
        {
            if (property == null)
                return;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;
            var value = EditorGUILayout.Vector3Field(label, property.vectorValue);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                property.vectorValue = new Vector4(value.x, value.y, value.z, property.vectorValue.w);
        }
    }
}
#endif
