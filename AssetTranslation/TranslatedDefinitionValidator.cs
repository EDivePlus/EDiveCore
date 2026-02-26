// Author: František Holubec
// Created: 16.04.2025

#if UNITY_EDITOR
using System.Linq;
using EDIVE.AssetTranslation;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEditor;
using UnityEngine;

[assembly: RegisterValidator(typeof(TranslatedDefinitionValidator<>))]
namespace EDIVE.AssetTranslation
{
    public class TranslatedDefinitionValidator<TDefinition> : RootObjectValidator<TDefinition> where TDefinition : ScriptableObject, IUniqueDefinition
    {
        protected override void Validate(ValidationResult result)
        {
            if (!AssetTranslationConfig.Instance.TryGetTranslator(typeof(TDefinition), out var translator))
                return;

            // Check if the asset is within the translator's filter folders
            var assetPath = AssetDatabase.GetAssetPath(Value);
            var filterFolders = translator.FilterFolders?.ToList();
            
            if (filterFolders != null && filterFolders.Any())
            {
                if (!filterFolders.Any(folder => !string.IsNullOrEmpty(folder) && assetPath.StartsWith(folder)))
                    return;
            }

            if (!translator.Contains(Value))
            {
                if (translator.RequireAllAssets)
                {
                    result.AddError("Translator does not contain this definition!")
                        .WithMetaData("Translator", translator)
                        .WithFix(() => translator.Add(Value));
                }
                else
                {
                    result.AddWarning("Translator does not contain this definition!")
                        .WithButton("Add", () => translator.Add(Value));
                }
            }
        }
    }
}
#endif
