using EDIVE.OdinExtensions.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace EDIVE.Localization.Fonts
{
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(TMP_Text))]
    [ExecuteAlways]
    public class LocalizeFont : MonoBehaviour
    {
        private static readonly string[] FONT_PROPERTIES = {"m_fontAsset", "m_sharedMaterial"};
        private static readonly string[] METRIC_PROPERTIES = {"m_fontSize", "m_fontSizeBase", "m_fontSizeMin", "m_fontSizeMax", "m_lineSpacing", "m_characterSpacing"};

        [SerializeField]
        [EnhancedInlineEditor]
        private LocalizedFontDefinition _Definition;

        private TMP_Text _text;
        private LocalizedFontDefinition _boundDefinition;
        private bool _hasBaseMetrics;
        private float _baseFontSize;
        private float _baseFontSizeMin;
        private float _baseFontSizeMax;
        private float _baseLineSpacing;
        private float _baseCharacterSpacing;

        public LocalizedFontDefinition Definition
        {
            get => _Definition;
            set
            {
                _Definition = value;
                Rebind();
            }
        }

        private TMP_Text Text => _text != null ? _text : _text = GetComponent<TMP_Text>();

        private void OnEnable() => Bind();
        private void OnDisable() => Unbind();

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_boundDefinition != _Definition)
                UnityEditor.EditorApplication.delayCall += Rebind;
        }
#endif

        private void Rebind()
        {
            if (this == null)
                return;

            Unbind();
            if (isActiveAndEnabled)
                Bind();
        }

        private void Bind()
        {
            if (_Definition == null)
                return;

            _boundDefinition = _Definition;
            _boundDefinition.Register(Apply);
        }

        private void Unbind()
        {
            // Unregister even from a destroyed definition so it drops this listener.
            if (ReferenceEquals(_boundDefinition, null))
                return;

            _boundDefinition.Unregister(Apply);
            _boundDefinition = null;
        }

        private void Apply()
        {
            var font = _boundDefinition.CurrentFont;
            if (font == null)
            {
                // Values were reverted, the authored ones may change before the next apply.
                _hasBaseMetrics = false;
                return;
            }

            var text = Text;
            RegisterDriven(text, FONT_PROPERTIES);
            text.font = font;
            text.fontSharedMaterial = _boundDefinition.CurrentMaterial;

            ApplyMetrics(text, _boundDefinition.CurrentOverride);
        }

        private void ApplyMetrics(TMP_Text text, LocalizedFontDefinition.LocaleOverride localeOverride)
        {
            var sizeScale = localeOverride?.SizeScale ?? 1f;
            var lineSpacing = localeOverride?.LineSpacing ?? 0f;
            var characterSpacing = localeOverride?.CharacterSpacing ?? 0f;

            var isNeutral = Mathf.Approximately(sizeScale, 1f) && lineSpacing == 0f && characterSpacing == 0f;
            if (isNeutral && !_hasBaseMetrics)
                return;

            if (!_hasBaseMetrics)
            {
                _baseFontSize = text.fontSize;
                _baseFontSizeMin = text.fontSizeMin;
                _baseFontSizeMax = text.fontSizeMax;
                _baseLineSpacing = text.lineSpacing;
                _baseCharacterSpacing = text.characterSpacing;
                _hasBaseMetrics = true;
            }

            RegisterDriven(text, METRIC_PROPERTIES);
            text.fontSize = _baseFontSize * sizeScale;
            text.fontSizeMin = _baseFontSizeMin * sizeScale;
            text.fontSizeMax = _baseFontSizeMax * sizeScale;
            text.lineSpacing = _baseLineSpacing + lineSpacing;
            text.characterSpacing = _baseCharacterSpacing + characterSpacing;
        }

        private static void RegisterDriven(Object target, string[] propertyPaths)
        {
            foreach (var propertyPath in propertyPaths)
                EditorPropertyDriver.RegisterProperty(target, propertyPath);
        }
    }
}
