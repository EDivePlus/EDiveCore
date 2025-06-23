using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Localization.Settings;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.Localization.View
{
    public class LocaleButtonsConstructor : MonoBehaviour
    {
        [SerializeField]
        private LocaleButton _ButtonPrefab;

        [SerializeField]
        private RectTransform _ButtonsRoot;

#if UNITY_EDITOR
        [Button]
        public void PopulateButtons()
        {
            _ButtonsRoot.DestroyChildrenImmediate();
            var languages = LocalizationSettings.AvailableLocales.Locales;
            foreach (var language in languages)
            {
                if (language.Metadata.HasMetadata<IgnoreLocale>())
                    continue;

                var instance = PrefabUtility.InstantiatePrefab(_ButtonPrefab, _ButtonsRoot) as LocaleButton;
                if (instance == null)
                    continue;

                instance.SetLocale(language);
                instance.gameObject.name = $"Button {language.GetEnglishName()}";
            }
        }
#endif
    }
}

