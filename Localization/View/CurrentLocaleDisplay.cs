using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace EDIVE.Localization.View
{
    public class CurrentLocaleDisplay : LocaleDisplay
    {
        protected override void Awake()
        {
            _Locale = LocalizationSettings.SelectedLocale;
            LocalizationSettings.SelectedLocaleChanged += LocalizationChanged;
            base.Awake();
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
