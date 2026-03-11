using EDIVE.VisualPresets.Switchers;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace EDIVE.Localization.View
{
    public class LocaleDisplay : MonoBehaviour
    {
        [SerializeField]
        protected EnhancedLocale _DefaultLocale;

        [SerializeField]
        private TMP_Text _LocaleNameText;

        [SerializeField]
        private VisualSwitcher _Visuals;

        public Locale CurrentLocale { get; private set; }

        protected virtual void Awake()
        {
            RefreshVisual();
            CurrentLocale = _DefaultLocale;
        }

        public void SetLocale(Locale locale)
        {
            if (locale == null)
                return;
            CurrentLocale = locale;
            RefreshVisual();
        }

        protected void RefreshVisual()
        {
            if (_DefaultLocale == null)
                return;
            
            if (_LocaleNameText != null) 
                _LocaleNameText.text = CurrentLocale.GetNativeName();

            if (CurrentLocale is EnhancedLocale enhancedLocale)
            {
                _Visuals?.Apply(enhancedLocale.VisualPreset);
            }
        }
    }
}
