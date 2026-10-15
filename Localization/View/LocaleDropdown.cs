// Author: Michal Petr
// Created: 11.03.2026

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace EDIVE.Localization.View
{
    public class LocaleDropdown : MonoBehaviour
    {
        [SerializeField]
        private TMP_Dropdown _Dropdown;
        
        [SerializeField]
        private bool _UseCustomLocaleList;
        
        [SerializeField]
        [EnhancedAssetList]
        [ShowIf(nameof(_UseCustomLocaleList))]
        private List<Locale> _AvailableLocales;

        // Sprite of each locale's visual preset shown in the options, e.g. its flag
        [SerializeField]
        private SpriteVisualID _IconID;
        
        public List<Locale> Locales => _UseCustomLocaleList ? _AvailableLocales : LocalizationSettings.AvailableLocales.Locales;

        private void Awake()
        {
            _Dropdown.ClearOptions();
            _Dropdown.AddOptions(Locales.Select(l => new TMP_Dropdown.OptionData(GetLanguageName(l), GetIcon(l), Color.white)).ToList());
        }

        // Native language name without the region, capitalized, e.g. Português
        private static string GetLanguageName(Locale locale)
        {
            var culture = locale.Identifier.CultureInfo;
            if (culture == null)
                return locale.GetNativeName();
            if (!culture.IsNeutralCulture && culture.Parent != CultureInfo.InvariantCulture)
                culture = culture.Parent;
            var name = culture.NativeName;
            return string.IsNullOrEmpty(name) ? name : culture.TextInfo.ToUpper(name[0]) + name.Substring(1);
        }

        private Sprite GetIcon(Locale locale)
        {
            if (_IconID == null || locale is not EnhancedLocale enhanced)
                return null;
            return enhanced.VisualPreset.TryGetRecord(_IconID, out SpriteVisualPresetRecord record) ? record.Sprite : null;
        }

        private void OnEnable()
        {
            _Dropdown.onValueChanged.AddListener(OnLocaleChanged);
            var selectedLocale = LocalizationSettings.SelectedLocale;
            var selectedIndex = Locales.IndexOf(selectedLocale);
            if (selectedIndex >= 0)
                _Dropdown.value = selectedIndex;
        }

        private void OnDisable()
        {
            _Dropdown.onValueChanged.RemoveListener(OnLocaleChanged);
        }
        
        private void OnLocaleChanged(int index)
        {
            if (index < 0 || index >= Locales.Count)
                return;
            LocalizationSettings.SelectedLocale = Locales[index];
        }
    }
}
