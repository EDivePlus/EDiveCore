// Author: František Holubec
// Created: 16.04.2025

#if UNITY_EDITOR
using EDIVE.AssetTranslation;
using Sirenix.OdinInspector.Editor.Validation;
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
