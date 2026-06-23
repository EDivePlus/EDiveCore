// Author: František Holubec
// Created: 13.04.2025

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.AssetTranslation
{
    public class AssetTranslationConfig : ScriptableObject
    {
        [EnhancedValidate("ValidateTranslators")]
        [EnhancedAssetList]
        [SerializeField]
        private List<ADefinitionTranslator> _Translators = new();

        private static AssetTranslationConfig _instance;

        public static AssetTranslationConfig Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                if (PreloadedAssets.TryGet<AssetTranslationConfig>(out var preloaded))
                    return _instance = preloaded;

#if UNITY_EDITOR
                _instance = EditorAssetUtils.FindAllAssetsOfType<AssetTranslationConfig>()?.FirstOrDefault();
#endif
                return _instance;
            }
        }

        public bool TryGetTranslator(Type type, out ADefinitionTranslator result)
        {
            return _Translators.TryGetFirst(t => t.DefinitionType.IsAssignableFrom(type), out result);
        }

        public bool TryGetTranslator<TTranslator>(out TTranslator result) where TTranslator : ADefinitionTranslator
        {
            return _Translators.TryGetFirstT(out result);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private void ValidateTranslators(List<ADefinitionTranslator> translators, SelfValidationResult result, InspectorProperty property)
        {
            if (translators == null)
                return;

            var duplicateTranslators = translators
                .Where(d => d != null)
                .GroupBy(d => d.GetType())
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateTranslators.Count > 0)
            {
                var builder = new StringBuilder();
                builder.Append($"There are multiple translators of the same type: [{string.Join(", ", duplicateTranslators.Select(s => s.Key.Name))}]");
                result.AddError(builder.ToString());
            }
        }
#endif
    }
}
