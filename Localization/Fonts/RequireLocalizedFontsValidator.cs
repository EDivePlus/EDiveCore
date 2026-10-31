#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using EDIVE.EditorUtils;
using EDIVE.Localization.Fonts;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.Utilities.Editor;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;

[assembly: RegisterValidationRule(typeof(RequireLocalizedFontsValidator), "Require Localized Fonts", "Check if localized TMP_Text has LocalizeFont matching its definition")]

namespace EDIVE.Localization.Fonts
{
    [Serializable]
    public class RequireLocalizedFontsValidator : RootObjectValidator<TMP_Text>
    {
        [SerializeField]
        [FolderPath]
        private List<string> _PathExceptions;

        [EnumToggleButtons]
        [SerializeField]
        private ValidatorSeverity _Severity;

        protected override void Validate(ValidationResult result)
        {
            if (CheckIgnoreAsset())
                return;

            if (Object.TryGetComponent<LocalizeFont>(out var localizeFont))
            {
                ValidateMatching(result, localizeFont);
                return;
            }

            if (!IsLocalized(Object))
                return;

            // Instances get it on the original prefab so every use inherits it.
            var target = GetFixTarget(Object);
            var message = target == Object ? "Localized text is missing LocalizeFont" : $"Localized text is missing LocalizeFont, fix adds it to '{target.transform.root.name}'";
            result.Add(_Severity, message)
                .WithFix((FixArgs args) =>
                {
                    if (target.TryGetComponent<LocalizeFont>(out _))
                        return;

                    var definition = args.GetMatchingPreset(target);
                    if (definition == null)
                    {
                        Debug.LogWarning($"No font definition matches '{target.name}'", target);
                        return;
                    }

                    Undo.AddComponent<LocalizeFont>(target.gameObject).Definition = definition;
                    if (PrefabUtility.IsPartOfPrefabAsset(target))
                        PrefabUtility.SavePrefabAsset(target.transform.root.gameObject);
                });
        }

        private void ValidateMatching(ValidationResult result, LocalizeFont localizeFont)
        {
            var definition = localizeFont.Definition;
            if (definition == null)
            {
                result.Add(_Severity, "LocalizeFont has no definition");
                return;
            }

            // Edit mode preview drives the text with the previewed locale.
            var locale = LocalizationSettings.SelectedLocale != null ? LocalizationSettings.SelectedLocale : LocalizationSettings.ProjectLocale;
            var font = definition.GetFontInEditor(locale);
            if (font == null)
                return;

            var material = definition.GetMaterial(font);
            if (Object.font == font && Object.fontSharedMaterial == material)
                return;

            result.Add(_Severity, $"Font differs from '{definition.name}'")
                .WithFix("Apply definition font", () =>
                {
                    Object.font = font;
                    Object.fontSharedMaterial = material;
                });
        }

        private static TMP_Text GetFixTarget(TMP_Text text)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(text))
                return text;

            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(text);
            return source != null ? source : text;
        }

        private static bool IsLocalized(TMP_Text text)
        {
            if (text.TryGetComponent<LocalizeStringEvent>(out _))
                return true;

            foreach (var stringEvent in text.GetComponentsInParent<LocalizeStringEvent>(true))
            {
                var updateEvent = stringEvent.OnUpdateString;
                for (var i = 0; i < updateEvent.GetPersistentEventCount(); i++)
                {
                    if (updateEvent.GetPersistentTarget(i) == text)
                        return true;
                }
            }

            return false;
        }

        private bool CheckIgnoreAsset()
        {
            if (Object == null)
                return true;

            var assetPath = AssetDatabase.GetAssetPath(Object);
            if (string.IsNullOrEmpty(assetPath))
                return false;

            if (_PathExceptions == null)
                return false;

            foreach (var pathException in _PathExceptions)
            {
                if (string.IsNullOrEmpty(pathException))
                    continue;

                if (assetPath.StartsWith(pathException))
                    return true;
            }

            return false;
        }

        [Serializable]
        private class FixArgs
        {
            [SerializeField]
            private bool _AutoDetectPreset = true;

            [SerializeField]
            [HideIf(nameof(_AutoDetectPreset))]
            private LocalizedFontDefinition _DefaultPreset;

            [SerializeField]
            [ShowIf(nameof(_AutoDetectPreset))]
            [ListDrawerSettings(ShowFoldout = false, OnTitleBarGUI = nameof(OnPresetsTitleBarGUI))]
            private List<LocalizedFontDefinition> _AvailablePresets;

            public LocalizedFontDefinition GetMatchingPreset(TMP_Text text)
            {
                if (_AutoDetectPreset && _AvailablePresets != null)
                {
                    foreach (var availablePreset in _AvailablePresets)
                    {
                        if (!availablePreset.IsMatching(text.font, text.fontSharedMaterial))
                            continue;

                        return availablePreset;
                    }
                }

                return _DefaultPreset;
            }

            private void OnPresetsTitleBarGUI()
            {
                if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
                {
                    RefreshPresets();
                }
            }

            [OnInspectorInit]
            private void RefreshPresets()
            {
                _AvailablePresets = EditorAssetUtils.FindAllAssetsOfType<LocalizedFontDefinition>();
            }
        }
    }
}
#endif
