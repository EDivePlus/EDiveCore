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

[assembly: RegisterValidationRule(typeof(RequireLocalizedFontsValidator), "Require Localized Fonts", "Check if TMP_Text component has LocalizeFont component attached to it", false)]
namespace EDIVE.Localization.Fonts
{
    [Serializable]
    public class RequireLocalizedFontsValidator : RootObjectValidator<TMP_Text>
    {
        [EnumToggleButtons]
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

            if (Object.TryGetComponent<LocalizeFont>(out _) || Object.TryGetComponent<DontLocalizeFont>(out _))
                return;

            result.Add(_Severity, "Missing font localization component!")
                .WithFix((FixArgs args) =>
                {
                    if (Object.TryGetComponent<LocalizeFont>(out var localize))
                        return;

                    if (args.Localize)
                    {
                        localize = Object.gameObject.AddComponent<LocalizeFont>();
                        localize.Definition = args.GetMatchingPreset(Object);
                    }
                    else
                    {
                        Object.gameObject.AddComponent<DontLocalizeFont>();
                    }
                });
        }

        private bool CheckIgnoreAsset()
        {
            if (Object == null)
                return true;

            if (PrefabUtility.IsPartOfPrefabInstance(Object))
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
            private bool _DontLocalize = false;

            [HideIfGroup("Localize", Condition = nameof(_DontLocalize))]
            [SerializeField]
            private bool _AutoDetectPreset = true;

            [HideIfGroup("Localize")]
            [SerializeField]
            [HideIf(nameof(_AutoDetectPreset))]
            private LocalizedFontDefinition _DefaultPreset;

            [HideIfGroup("Localize")]
            [SerializeField]
            [ShowIf(nameof(_AutoDetectPreset))]
            [ListDrawerSettings(ShowFoldout = false, OnTitleBarGUI = nameof(OnPresetsTitleBarGUI))]
            private List<LocalizedFontDefinition> _AvailablePresets;

            public bool Localize => !_DontLocalize;

            public LocalizedFontDefinition GetMatchingPreset(TMP_Text text)
            {
                if (_AutoDetectPreset)
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
