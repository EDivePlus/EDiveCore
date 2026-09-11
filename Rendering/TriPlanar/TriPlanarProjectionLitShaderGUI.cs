#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;

namespace VillaStiassni.Editor
{
    /// <summary>
    /// URP Lit inspector for Custom/TriPlanarProjectionLit: the stock Surface Options,
    /// Surface Inputs and Advanced foldouts, with the UV tiling replaced by the
    /// triplanar projection and a Detail Inputs section that has its own tiling.
    /// </summary>
    public class TriPlanarProjectionLitShaderGUI : BaseShaderGUI
    {
        private static class Styles
        {
            public static readonly GUIContent Projection = EditorGUIUtility.TrTextContent("Triplanar Projection");
            public static readonly GUIContent Space = EditorGUIUtility.TrTextContent("Space",
                "Project from world space, or from the renderer's local space so the texture follows the object.");
            public static readonly GUIContent Tiling = EditorGUIUtility.TrTextContent("Tiling",
                "Texture repeats per unit of projection space, per axis.");
            public static readonly GUIContent Offset = EditorGUIUtility.TrTextContent("Offset",
                "Shifts the projection origin, in projection space units.");
            public static readonly GUIContent BlendSharpness = EditorGUIUtility.TrTextContent("Blend Sharpness",
                "How tightly the three planes blend on slopes. Higher is sharper.");

            public static readonly GUIContent DetailInputs = EditorGUIUtility.TrTextContent("Detail Inputs",
                "Overlaid at a second, independent tiling.");
            public static readonly GUIContent DetailMask = EditorGUIUtility.TrTextContent("Mask",
                "Alpha masks where the detail maps are applied. Sampled at the base tiling, like URP Lit.");
            public static readonly GUIContent DetailAlbedo = EditorGUIUtility.TrTextContent("Base Map",
                "Overlay multiplied over the base albedo. Mid grey (0.5) leaves it unchanged.");
            public static readonly GUIContent DetailNormal = EditorGUIUtility.TrTextContent("Normal Map",
                "Blended onto the base normal per projection plane.");
            public static readonly GUIContent DetailTiling = EditorGUIUtility.TrTextContent("Tiling",
                "Detail repeats per unit of projection space. Pick a value that is not a whole multiple of the base tiling.");
            public static readonly GUIContent DetailOffset = EditorGUIUtility.TrTextContent("Offset",
                "Shifts the detail layer relative to the projection origin, in projection space units.");
        }

        private LitGUI.LitProperties m_LitProperties;

        private MaterialProperty m_ProjectionSpace;
        private MaterialProperty m_Tiling;
        private MaterialProperty m_ProjectionOffset;
        private MaterialProperty m_BlendSharpness;

        private MaterialProperty m_DetailMask;
        private MaterialProperty m_DetailAlbedoMap;
        private MaterialProperty m_DetailAlbedoMapScale;
        private MaterialProperty m_DetailNormalMap;
        private MaterialProperty m_DetailNormalMapScale;
        private MaterialProperty m_DetailTiling;
        private MaterialProperty m_DetailOffset;

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            m_LitProperties = new LitGUI.LitProperties(properties);

            m_ProjectionSpace = FindProperty("_ProjectionSpace", properties, false);
            m_Tiling = FindProperty("_Tiling", properties, false);
            m_ProjectionOffset = FindProperty("_ProjectionOffset", properties, false);
            m_BlendSharpness = FindProperty("_BlendSharpness", properties, false);

            m_DetailMask = FindProperty("_DetailMask", properties, false);
            m_DetailAlbedoMap = FindProperty("_DetailAlbedoMap", properties, false);
            m_DetailAlbedoMapScale = FindProperty("_DetailAlbedoMapScale", properties, false);
            m_DetailNormalMap = FindProperty("_DetailNormalMap", properties, false);
            m_DetailNormalMapScale = FindProperty("_DetailNormalMapScale", properties, false);
            m_DetailTiling = FindProperty("_DetailTiling", properties, false);
            m_DetailOffset = FindProperty("_DetailOffset", properties, false);
        }

        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, LitGUI.SetMaterialKeywords, SetDetailKeywords);
        }

        private static void SetDetailKeywords(Material material)
        {
            bool scaled = material.GetFloat("_DetailAlbedoMapScale") != 1.0f;
            bool hasDetail = material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap");
            CoreUtils.SetKeyword(material, "_DETAIL_MULX2", !scaled && hasDetail);
            CoreUtils.SetKeyword(material, "_DETAIL_SCALED", scaled && hasDetail);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            EditorGUIUtility.labelWidth = 0f;
            if (m_LitProperties.workflowMode != null)
                DoPopup(LitGUI.Styles.workflowModeText, m_LitProperties.workflowMode, System.Enum.GetNames(typeof(LitGUI.WorkflowMode)));

            base.DrawSurfaceOptions(material);
        }

        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            LitGUI.Inputs(m_LitProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawProjection();
        }

        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList)
        {
            materialScopesList.RegisterHeaderScope(Styles.DetailInputs, (uint)Expandable.Details, _ => DrawDetailInputs());
        }

        public override void DrawAdvancedOptions(Material material)
        {
            if (m_LitProperties.highlights != null)
                materialEditor.ShaderProperty(m_LitProperties.highlights, LitGUI.Styles.highlightsText);
            if (m_LitProperties.reflections != null)
                materialEditor.ShaderProperty(m_LitProperties.reflections, LitGUI.Styles.reflectionsText);

            base.DrawAdvancedOptions(material);
        }

        private void DrawProjection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.Projection, EditorStyles.boldLabel);

            if (m_ProjectionSpace != null)
                materialEditor.ShaderProperty(m_ProjectionSpace, Styles.Space);
            if (m_Tiling != null)
                DrawVector3(m_Tiling, Styles.Tiling);
            if (m_ProjectionOffset != null)
                DrawVector3(m_ProjectionOffset, Styles.Offset);
            if (m_BlendSharpness != null)
                materialEditor.ShaderProperty(m_BlendSharpness, Styles.BlendSharpness);
        }

        private void DrawDetailInputs()
        {
            materialEditor.TexturePropertySingleLine(Styles.DetailMask, m_DetailMask);
            materialEditor.TexturePropertySingleLine(Styles.DetailAlbedo, m_DetailAlbedoMap, m_DetailAlbedoMapScale);
            DrawNormalArea(materialEditor, m_DetailNormalMap, m_DetailNormalMapScale);
            if (m_DetailTiling != null)
                DrawVector3(m_DetailTiling, Styles.DetailTiling);
            if (m_DetailOffset != null)
                DrawVector3(m_DetailOffset, Styles.DetailOffset);
        }

        // Vector properties are float4 in the shader; only XYZ mean anything here.
        private void DrawVector3(MaterialProperty property, GUIContent label)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;
            Vector3 value = EditorGUILayout.Vector3Field(label, property.vectorValue);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                property.vectorValue = new Vector4(value.x, value.y, value.z, property.vectorValue.w);
        }
    }
}
#endif
