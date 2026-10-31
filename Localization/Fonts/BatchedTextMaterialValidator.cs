// Author: Michal Petr
// Created: 29.10.2026

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.EditorUtils;
using EDIVE.Localization.Fonts;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.Validation;
using TMPro;
using UnityEngine;

[assembly: RegisterValidationRule(typeof(BatchedTextMaterialValidator), "Batched Text Material", "Check if world space TextMeshPro uses a material with an allowed shader so it gets batched", false)]
namespace EDIVE.Localization.Fonts
{
    [Serializable]
    public class BatchedTextMaterialValidator : RootObjectValidator<TextMeshPro>
    {
        private static readonly string[] DEFAULT_SHADER_NAMES =
        {
            "TextMeshPro/SRP/TMP_SDF-URP Lit",
            "TextMeshPro/SRP/TMP_SDF-URP Unlit"
        };

        // Fix args can't access rule settings, so the last resolved list is shared here.
        private static List<Shader> _lastAllowedShaders;

        [SerializeField]
        [Tooltip("Empty uses the URP Lit and Unlit TMP shaders.")]
        private List<Shader> _AllowedShaders = new();

        [EnumToggleButtons]
        [SerializeField]
        private ValidatorSeverity _Severity = ValidatorSeverity.Warning;

        protected override void Validate(ValidationResult result)
        {
            if (Object == null)
                return;

            var allowedShaders = GetAllowedShaders();

            if (Object.TryGetComponent<LocalizeFont>(out var localizeFont) && localizeFont.Definition != null)
            {
                var definition = localizeFont.Definition;
                var materials = definition.StyleMaterial != null
                    ? new[] {definition.StyleMaterial}
                    : definition.GetAllFontsInEditor().Select(f => f.material);

                foreach (var definitionMaterial in materials)
                {
                    if (IsAllowed(definitionMaterial, allowedShaders))
                        continue;

                    result.Add(_Severity, $"Font material '{GetName(definitionMaterial)}' in '{definition.name}' is not batched");
                }
                return;
            }

            var material = Object.fontSharedMaterial;
            if (IsAllowed(material, allowedShaders))
                return;

            result.Add(_Severity, $"Material '{GetName(material)}' is not batched")
                .WithFix((FixArgs args) =>
                {
                    var shader = args.Shader != null ? args.Shader : allowedShaders.FirstOrDefault();
                    var replacement = FindReplacement(Object.font, shader);
                    if (replacement == null)
                    {
                        Debug.LogWarning($"No '{GetName(shader)}' material found for font '{GetName(Object.font)}'", Object);
                        return;
                    }

                    Object.fontSharedMaterial = replacement;
                });
        }

        private List<Shader> GetAllowedShaders()
        {
            _lastAllowedShaders = _AllowedShaders != null && _AllowedShaders.Count > 0
                ? _AllowedShaders
                : DEFAULT_SHADER_NAMES.Select(Shader.Find).Where(s => s != null).ToList();
            return _lastAllowedShaders;
        }

        private static bool IsAllowed(Material material, List<Shader> allowedShaders)
        {
            return material != null && allowedShaders.Contains(material.shader);
        }

        private static Material FindReplacement(TMP_FontAsset font, Shader shader)
        {
            if (font == null || shader == null)
                return null;

            return EditorAssetUtils.FindAllAssetsOfType<Material>()
                .FirstOrDefault(m => m != null && m.shader == shader && m.HasProperty(ShaderUtilities.ID_MainTex) && m.GetTexture(ShaderUtilities.ID_MainTex) == font.atlasTexture);
        }

        private static string GetName(UnityEngine.Object obj) => obj != null ? obj.name : "None";

        [Serializable]
        private class FixArgs
        {
            [SerializeField]
            [ValueDropdown("GetShaders")]
            private Shader _Shader;

            public Shader Shader => _Shader;

            [OnInspectorInit]
            private void Init()
            {
                if (_Shader == null)
                    _Shader = _lastAllowedShaders?.FirstOrDefault();
            }

            [UsedImplicitly]
            private IEnumerable<ValueDropdownItem<Shader>> GetShaders()
            {
                return _lastAllowedShaders?.Where(s => s != null).Select(s => new ValueDropdownItem<Shader>(s.name, s))
                    ?? Enumerable.Empty<ValueDropdownItem<Shader>>();
            }
        }
    }
}
#endif
