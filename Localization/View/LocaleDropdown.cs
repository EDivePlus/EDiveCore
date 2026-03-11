// Author: Michal Petr
// Created: 11.03.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
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
        
        public List<Locale> Locales => _UseCustomLocaleList ? _AvailableLocales : LocalizationSettings.AvailableLocales.Locales;

        private void Awake()
        {
            _Dropdown.ClearOptions();
            _Dropdown.AddOptions(Locales.Select(l => l.GetNativeName()).ToList());
        }

        private void OnEnable()
        {
            _Dropdown.onValueChanged.AddListener(OnLocaleChanged);
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
