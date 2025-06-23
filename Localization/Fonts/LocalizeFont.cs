using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace EDIVE.Localization.Fonts
{
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(TMP_Text))]
    [ExecuteAlways]
    public class LocalizeFont : MonoBehaviour
    {
        [SerializeField]
        private LocalizedFontDefinition _Definition;

        public LocalizedFontDefinition Definition
        {
            get => _Definition;
            set => _Definition = value;
        }

        private void Awake()
        {
            Refresh();
            LocalizationSettings.SelectedLocaleChanged += Refresh;
        }

        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= Refresh;
        }

        private void Refresh(Locale newLocale) => Refresh();

        [Button]
        private void Refresh()
        {
            if (Definition == null)
                return;

            var text = GetComponent<TMP_Text>();

#if UNITY_WEBGL
            var localeOperation = LocalizationSettings.Instance.GetSelectedLocaleAsync();
            if (!localeOperation.IsDone)
                return;
            
            var currentLanguage = (localeOperation.Result ?? LocalizationSettings.ProjectLocale).GetEnglishName();
#else
            var currentLanguage = (LocalizationSettings.Instance.GetSelectedLocale() ?? LocalizationSettings.ProjectLocale).GetEnglishName();
#endif

            
            var preset = Definition.GetLanguageFontPreset(currentLanguage);
            text.font = preset.Font;
            text.fontSharedMaterial = preset.Material;
        }
    }
}
