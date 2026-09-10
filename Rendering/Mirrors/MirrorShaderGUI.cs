#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace EDIVE.Rendering.Mirrors
{
    public class MirrorShaderGUI : BaseShaderGUI
    {
        private static readonly GUIContent MASK_MAP = EditorGUIUtility.TrTextContent("Mask Map", "R reflectivity, A smoothness.");
        private static readonly GUIContent BASE_SCROLL = EditorGUIUtility.TrTextContent("Base Map Scroll", "UV units per second.");
        private static readonly GUIContent NORMAL_SCROLL = EditorGUIUtility.TrTextContent("Normal Map Scroll", "UV units per second.");
        private static readonly GUIContent ALPHA = EditorGUIUtility.TrTextContent("Alpha", "Transparent surfaces only.");

        private static readonly GUIContent CAMERA_HEADER = EditorGUIUtility.TrTextContent("Camera Reflection");
        private static readonly GUIContent LEFT = EditorGUIUtility.TrTextContent("Left", "Set at runtime.");
        private static readonly GUIContent RIGHT = EditorGUIUtility.TrTextContent("Right", "Set at runtime.");
        private static readonly GUIContent TINT = EditorGUIUtility.TrTextContent("Tint");
        private static readonly GUIContent REFLECTIVITY = EditorGUIUtility.TrTextContent("Reflectivity", "Strength head on. Always full at grazing angles.");
        private static readonly GUIContent FRESNEL_POWER = EditorGUIUtility.TrTextContent("Fresnel Power", "How fast it ramps up at grazing angles.");
        private static readonly GUIContent BLUR = EditorGUIUtility.TrTextContent("Blur");
        private static readonly GUIContent REFRACTION = EditorGUIUtility.TrTextContent("Refraction", "Bends the reflection with the normal map.");

        private static readonly GUIContent FALLBACK_HEADER = EditorGUIUtility.TrTextContent("Fallback");
        private static readonly GUIContent SOURCE = EditorGUIUtility.TrTextContent("Environment", "What the fallback material reflects.");
        private static readonly GUIContent FALLBACK_ENV_COLOR = EditorGUIUtility.TrTextContent("Color");
        private static readonly GUIContent BOX_PROJECTION = EditorGUIUtility.TrTextContent("Box Projection", "Fit the probe to its box.");
        private static readonly GUIContent FALLBACK_COLOR = EditorGUIUtility.TrTextContent("Base Color");
        private static readonly GUIContent METALLIC = EditorGUIUtility.TrTextContent("Metallic", "1 is a mirror tinted by Base Color. 0 is a plain diffuse surface.");
        private static readonly GUIContent SMOOTHNESS = EditorGUIUtility.TrTextContent("Smoothness");

        private static readonly string[] SOURCE_NAMES = { "Color", "Reflection Probe" };

        private const string BUMP_MAP_PROP = "_BumpMap";
        private const string BUMP_SCALE_PROP = "_BumpScale";
        private const string NORMAL_SPEED_PROP = "_NormalSpeed";
        private const string MASK_MAP_PROP = "_MaskMap";
        private const string ALBEDO_SPEED_PROP = "_AlbedoSpeed";
        private const string ALPHA_PROP = "_Alpha";
        private const string TEX_LEFT_PROP = "_MirrorTexLeft";
        private const string TEX_RIGHT_PROP = "_MirrorTexRight";
        private const string REFLECTION_TINT_PROP = "_ReflectionTint";
        private const string REFLECTIVITY_PROP = "_Reflectivity";
        private const string FRESNEL_POWER_PROP = "_FresnelPower";
        private const string BLUR_PROP = "_Blur";
        private const string REFRACTION_PROP = "_Refraction";
        private const string PROBE_FALLBACK_PROP = "_ProbeFallback";
        private const string FALLBACK_ENV_COLOR_PROP = "_FallbackEnvColor";
        private const string BOX_PROJECTION_PROP = "_BoxProjection";
        private const string FALLBACK_COLOR_PROP = "_FallbackColor";
        private const string METALLIC_PROP = "_Metallic";
        private const string SMOOTHNESS_PROP = "_Smoothness";

        private const string MASK_MAP_KEYWORD = "_MASKMAP";
        private const string BLUR_KEYWORD = "_BLUR_ON";
        private const string PROBE_FALLBACK_KEYWORD = "_PROBE_FALLBACK";
        private const string BOX_PROJECTION_KEYWORD = "_BOXPROJECTION_ON";

        // FindProperty needs the name, material reads want the id.
        private static readonly int MASK_MAP_ID = Shader.PropertyToID(MASK_MAP_PROP);
        private static readonly int BLUR_ID = Shader.PropertyToID(BLUR_PROP);
        private static readonly int PROBE_FALLBACK_ID = Shader.PropertyToID(PROBE_FALLBACK_PROP);
        private static readonly int BOX_PROJECTION_ID = Shader.PropertyToID(BOX_PROJECTION_PROP);

        private MaterialProperty _bumpMap;
        private MaterialProperty _bumpScale;
        private MaterialProperty _normalSpeed;
        private MaterialProperty _maskMap;
        private MaterialProperty _albedoSpeed;
        private MaterialProperty _alpha;

        private MaterialProperty _texLeft;
        private MaterialProperty _texRight;
        private MaterialProperty _reflectionTint;
        private MaterialProperty _reflectivity;
        private MaterialProperty _fresnelPower;
        private MaterialProperty _blur;
        private MaterialProperty _refraction;

        private MaterialProperty _probeFallback;
        private MaterialProperty _fallbackEnvColor;
        private MaterialProperty _boxProjection;
        private MaterialProperty _fallbackColor;
        private MaterialProperty _metallic;
        private MaterialProperty _smoothness;

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);

            _bumpMap = FindProperty(BUMP_MAP_PROP, properties, false);
            _bumpScale = FindProperty(BUMP_SCALE_PROP, properties, false);
            _normalSpeed = FindProperty(NORMAL_SPEED_PROP, properties, false);
            _maskMap = FindProperty(MASK_MAP_PROP, properties, false);
            _albedoSpeed = FindProperty(ALBEDO_SPEED_PROP, properties, false);
            _alpha = FindProperty(ALPHA_PROP, properties, false);

            _texLeft = FindProperty(TEX_LEFT_PROP, properties, false);
            _texRight = FindProperty(TEX_RIGHT_PROP, properties, false);
            _reflectionTint = FindProperty(REFLECTION_TINT_PROP, properties, false);
            _reflectivity = FindProperty(REFLECTIVITY_PROP, properties, false);
            _fresnelPower = FindProperty(FRESNEL_POWER_PROP, properties, false);
            _blur = FindProperty(BLUR_PROP, properties, false);
            _refraction = FindProperty(REFRACTION_PROP, properties, false);

            _probeFallback = FindProperty(PROBE_FALLBACK_PROP, properties, false);
            _fallbackEnvColor = FindProperty(FALLBACK_ENV_COLOR_PROP, properties, false);
            _boxProjection = FindProperty(BOX_PROJECTION_PROP, properties, false);
            _fallbackColor = FindProperty(FALLBACK_COLOR_PROP, properties, false);
            _metallic = FindProperty(METALLIC_PROP, properties, false);
            _smoothness = FindProperty(SMOOTHNESS_PROP, properties, false);
        }

        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, null, SetMirrorKeywords);
        }

        private static void SetMirrorKeywords(Material material)
        {
            CoreUtils.SetKeyword(material, MASK_MAP_KEYWORD, material.GetTexture(MASK_MAP_ID));
            CoreUtils.SetKeyword(material, BLUR_KEYWORD, material.GetFloat(BLUR_ID) > 0f);

            var probe = material.GetFloat(PROBE_FALLBACK_ID) > 0.5f;
            CoreUtils.SetKeyword(material, PROBE_FALLBACK_KEYWORD, probe);
            CoreUtils.SetKeyword(material, BOX_PROJECTION_KEYWORD, probe && material.GetFloat(BOX_PROJECTION_ID) > 0.5f);
        }

        public override void DrawSurfaceInputs(Material material)
        {
            DrawBaseProperties(material);
            DrawVector2(_albedoSpeed, BASE_SCROLL);

            EditorGUILayout.Space();
            DrawNormalArea(materialEditor, _bumpMap, _bumpScale);
            DrawVector2(_normalSpeed, NORMAL_SCROLL);

            EditorGUILayout.Space();
            materialEditor.TexturePropertySingleLine(MASK_MAP, _maskMap);
            Draw(_alpha, ALPHA);

            DrawTileOffset(materialEditor, baseMapProp);
        }

        // Two foldouts. Reflection and fallback are set up separately.
        private const uint CAMERA_FOLDOUT = 1u << 3;
        private const uint FALLBACK_FOLDOUT = 1u << 4;

        public override void FillAdditionalFoldouts(MaterialHeaderScopeList materialScopesList)
        {
            materialScopesList.RegisterHeaderScope(CAMERA_HEADER, CAMERA_FOLDOUT, _ => DrawCameraReflection());
            materialScopesList.RegisterHeaderScope(FALLBACK_HEADER, FALLBACK_FOLDOUT, _ => DrawFallback());
        }

        private void DrawCameraReflection()
        {
            materialEditor.TexturePropertySingleLine(LEFT, _texLeft);
            materialEditor.TexturePropertySingleLine(RIGHT, _texRight);
            Draw(_reflectionTint, TINT);
            Draw(_reflectivity, REFLECTIVITY);

            using (new EditorGUI.DisabledScope(_reflectivity == null || _reflectivity.floatValue >= 1f))
                Draw(_fresnelPower, FRESNEL_POWER);

            Draw(_blur, BLUR);

            using (new EditorGUI.DisabledScope(_bumpMap == null || _bumpMap.textureValue == null))
                Draw(_refraction, REFRACTION);
        }

        private void DrawFallback()
        {
            if (_probeFallback == null)
                return;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = _probeFallback.hasMixedValue;
            var source = EditorGUILayout.Popup(SOURCE, (int) _probeFallback.floatValue, SOURCE_NAMES);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                materialEditor.RegisterPropertyChangeUndo(SOURCE.text);
                _probeFallback.floatValue = source;
            }

            EditorGUI.indentLevel++;
            if (source == 0)
                Draw(_fallbackEnvColor, FALLBACK_ENV_COLOR);
            else
                Draw(_boxProjection, BOX_PROJECTION);
            EditorGUI.indentLevel--;

            Draw(_fallbackColor, FALLBACK_COLOR);
            Draw(_metallic, METALLIC);
            Draw(_smoothness, SMOOTHNESS);
        }

        private void Draw(MaterialProperty property, GUIContent label)
        {
            if (property != null)
                materialEditor.ShaderProperty(property, label);
        }

        // Vectors are float4. Only XY used.
        private void DrawVector2(MaterialProperty property, GUIContent label)
        {
            if (property == null)
                return;

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;
            var value = EditorGUILayout.Vector2Field(label, property.vectorValue);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                property.vectorValue = new Vector4(value.x, value.y, property.vectorValue.z, property.vectorValue.w);
        }
    }
}
#endif
