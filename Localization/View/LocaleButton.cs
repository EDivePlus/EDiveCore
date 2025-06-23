using EDIVE.StateHandling.ToggleStates;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace EDIVE.Localization.View
{
    [RequireComponent(typeof(Button))]
    public class LocaleButton : LocaleDisplay
    {
        [SerializeField]
        private AToggleState _SelectedToggle;
        
        private Button _button;
        private Button Button => _button != null ? _button : _button = GetComponent<Button>();

        protected override void Awake()
        {
            base.Awake();
            LocalizationChanged(LocalizationSettings.SelectedLocale);
            LocalizationSettings.SelectedLocaleChanged += LocalizationChanged;
            Button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            Button.onClick.RemoveListener(OnClick);
            LocalizationSettings.SelectedLocaleChanged -= LocalizationChanged;
        }

        private void OnClick()
        {
            ApplyLanguage();
        }

        private void LocalizationChanged(Locale locale)
        {
            if (_SelectedToggle)
            {
                _SelectedToggle.State = locale == Locale;
            }
            RefreshVisual();
        }
        
        private void ApplyLanguage()
        {
            if (Locale != null)
            {
                LocalizationSettings.SelectedLocale = Locale;
            }
        }
    }
}
