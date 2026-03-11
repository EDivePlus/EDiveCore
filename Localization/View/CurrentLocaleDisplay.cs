// Author: Michal Petr
// Created: 11.03.2026

using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace EDIVE.Localization.View
{
    public class CurrentLocaleDisplay : LocaleDisplay
    {
        protected override void Awake()
        {
            SetLocale(LocalizationSettings.SelectedLocale);
            LocalizationSettings.SelectedLocaleChanged += LocalizationChanged;
        }

        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= LocalizationChanged;
        }

        private void LocalizationChanged(Locale newLocale)
        {
            RefreshVisual();
        }
    }
}
