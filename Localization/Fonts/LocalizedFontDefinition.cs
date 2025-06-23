using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace EDIVE.Localization.Fonts
{
    public class LocalizedFontDefinition : ScriptableObject
    {
        [InlineProperty]
        [SerializeField]
        [HideLabel]
        [BoxGroup("Default Preset")]
        private FontPreset _DefaultPreset;

        [PropertySpace(6)]
        [SerializeField]
        [EnhancedTableList(ShowFoldout = false)]
        private List<LanguageOverridePreset> _LanguageOverrides;

        public FontPreset GetLanguageFontPreset(string languageID)
        {
            return _LanguageOverrides.TryGetFirst(p => p.Languages.Contains(languageID), out var result) ? result.Preset : _DefaultPreset;
        }

        public bool IsMatching(TMP_FontAsset fontAsset, Material material)
        {
            return _DefaultPreset.Font == fontAsset && _DefaultPreset.Material == material;
        }

        [Serializable]
        private class LanguageOverridePreset
        {
            [SerializeField]
            [ListDrawerSettings(ShowFoldout = false)]
            [LabelText(" ")]
            [ValueDropdown("GetAllLanguages", IsUniqueList = true)]
            private List<string> _Languages;

            [InlineProperty]
            [SerializeField]
            private FontPreset _Preset;

            public List<string> Languages => _Languages;
            public FontPreset Preset => _Preset;

            [UsedImplicitly]
            private IEnumerable GetAllLanguages() => LocalizationSettings.AvailableLocales.Locales.Select(locale => locale.GetEnglishName());
        }
    }
}
