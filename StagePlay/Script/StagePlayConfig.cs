// Author: František Holubec
// Created: 23.06.2025

using System;
using UnityEngine;
using UnityEngine.Localization;

namespace EDIVE.StagePlay.Script
{
    [Serializable]
    public class StagePlayConfig
    {
        [SerializeField]
        private Locale _MainTextLocale;

        [SerializeField]
        private Locale _SecondaryTextLocale;

        public Locale MainTextLocale => _MainTextLocale;
        public Locale SecondaryTextLocale => _SecondaryTextLocale;

        public StagePlayConfig(Locale mainTextLocale, Locale secondaryTextLocale)
        {
            _MainTextLocale = mainTextLocale;
            _SecondaryTextLocale = secondaryTextLocale;
        }
    }
}
