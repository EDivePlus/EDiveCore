using EDIVE.Localization.Fonts;
using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

namespace EDIVE.Localization.View
{
    public class LocaleDisplay : MonoBehaviour
    {
        [SerializeField]
        protected Locale _Locale;

        [SerializeField]
        private Image _FlagImage;

        [SerializeField]
        private AToggleState _FlagImageLoadedToggle;

        [SerializeField]
        private LocalizedSprite _FlagSpriteLocalizedAsset;

        [SerializeField]
        private TMP_Text _LabelText;

        [SerializeField]
        private LocalizedFontDefinition _Font;

        public Locale Locale => _Locale;

        protected virtual void Awake()
        {
            RefreshVisual();
        }

        public void SetLocale(Locale locale)
        {
            _Locale = locale;
            RefreshVisual();
        }

        [PropertySpace]
        [Button]
        protected void RefreshVisual()
        {
            if (_Locale == null)
                return;

            if(_FlagImage)
                ApplyFlagIcon();

            if (_LabelText)
            {
                _LabelText.text = _Locale.GetNativeName();
                if (_Font)
                {
                    var preset = _Font.GetLanguageFontPreset(_Locale.GetEnglishName());
                    _LabelText.font = preset.Font;
                    _LabelText.fontSharedMaterial = preset.Material;
                }
            }
        }
        
        private void ApplyFlagIcon()
        {
            if (_FlagSpriteLocalizedAsset != null)
            {
                if (_FlagImageLoadedToggle)
                    _FlagImageLoadedToggle.SetState(false);
                _FlagSpriteLocalizedAsset.LocaleOverride = _Locale;
                var loadOperation = _FlagSpriteLocalizedAsset.LoadAssetAsync();
                loadOperation.Completed += handle =>
                {
                    if (!this || !_FlagImage)
                        return;
                    _FlagImage.sprite = handle.Result;
                    if (_FlagImageLoadedToggle)
                        _FlagImageLoadedToggle.SetState(true);
                };
            }
        }
    }
}
