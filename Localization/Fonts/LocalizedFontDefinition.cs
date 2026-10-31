using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace EDIVE.Localization.Fonts
{
    public class LocalizedFontDefinition : ScriptableObject
    {
        [SerializeField]
        [Required]
        [EnhancedValidate("ValidateGlyphs")]
        private TMP_FontAsset _DefaultFont;

        [SerializeField]
        [Tooltip("Copied onto each locale font. Empty uses the font's default material.")]
        [EnhancedValueDropdown("GetAllFontMaterials", AppendNextDrawer = true, DontReloadOnInit = true)]
        private Material _StyleMaterial;

        [SerializeField]
        [Tooltip("Optional asset table entry for locales that need another font. Empty value uses the default font.")]
        private LocalizedTmpFont _LocaleFont = new();

        [SerializeField]
        [Tooltip("Characters not in the string tables that should be in the static atlas, e.g. symbols set from code.")]
        [EnhancedValidate("ValidateExtraCharacters")]
        [InlineIconButton(FontAwesomeEditorIconType.CopySolid, "CopyAtlasCharacters", "Copy atlas, string table and extra characters for the Font Asset Creator", GUIAlwaysEnabled = true)]
        private string _ExtraCharacters;

        [PropertySpace(6)]
        [SerializeField]
        [EnhancedTableList(ShowFoldout = false)]
        [EnhancedValidate("ValidateLocaleOverrides")]
        private List<LocaleOverride> _LocaleOverrides;

#if UNITY_EDITOR
        private List<LocalePreview> _preview;

        [ShowInInspector]
        [PropertySpace(6)]
        [EnhancedTableList(IsReadOnly = true, ShowFoldout = false)]
        private List<LocalePreview> Preview => _preview ??= LocalizationSettings.AvailableLocales.Locales.Select(GetPreview).ToList();
#endif

        private Action _changed;
        private AsyncOperationHandle<Locale> _pendingLocale;

        public TMP_FontAsset DefaultFont => _DefaultFont;
        public Material StyleMaterial => _StyleMaterial;

        public TMP_FontAsset CurrentFont { get; private set; }
        public Material CurrentMaterial { get; private set; }
        public LocaleOverride CurrentOverride { get; private set; }

        public void Register(Action callback)
        {
            var isFirst = _changed == null;
            _changed += callback;

            if (isFirst)
                Subscribe();
            else if (CurrentFont != null)
                callback();
        }

        public void Unregister(Action callback)
        {
            _changed -= callback;
            if (_changed == null)
                Unsubscribe();
        }

        public LocaleOverride GetOverride(Locale locale)
        {
            if (locale == null || _LocaleOverrides == null)
                return null;

            var code = locale.Identifier.Code;
            return _LocaleOverrides.FirstOrDefault(o => o?.Locales != null && o.Locales.Contains(code));
        }

        public Material GetMaterial(TMP_FontAsset font)
        {
            if (font == null)
                return null;

            var fontMaterial = font.material;
            if (_StyleMaterial == null)
                return fontMaterial;

            if (_StyleMaterial.HasProperty(ShaderUtilities.ID_MainTex) && _StyleMaterial.GetTexture(ShaderUtilities.ID_MainTex) == font.atlasTexture)
                return _StyleMaterial;

            // Copies the style onto another font atlas, shared per style and atlas.
            return fontMaterial != null ? TMP_MaterialManager.GetFallbackMaterial(_StyleMaterial, fontMaterial) : null;
        }

        private void OnDisable()
        {
            if (_changed == null)
                return;

            Unsubscribe();
            _changed = null;
        }

        private void NotifyChanged()
        {
            if (_changed == null)
                return;

            // One broken listener must not stop the others or the locale change handlers.
            foreach (var callback in _changed.GetInvocationList())
            {
                try
                {
                    ((Action) callback)();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            }
        }

        private void Subscribe()
        {
            if (!LocalizationSettings.HasSettings)
                return;

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            if (!_LocaleFont.IsEmpty)
            {
                _LocaleFont.AssetChanged += OnLocaleFontChanged;
                return;
            }

            var operation = LocalizationSettings.SelectedLocaleAsync;
            if (operation.IsDone)
            {
                OnLocaleChanged(operation.Result);
                return;
            }

            _pendingLocale = operation;
            _pendingLocale.Completed += OnLocaleLoaded;
        }

        private void Unsubscribe()
        {
            _LocaleFont.AssetChanged -= OnLocaleFontChanged;
            if (LocalizationSettings.HasSettings)
                LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            if (_pendingLocale.IsValid())
                _pendingLocale.Completed -= OnLocaleLoaded;

            _pendingLocale = default;
            Clear();
        }

        private void Clear()
        {
            CurrentFont = null;
            CurrentMaterial = null;
            CurrentOverride = null;
        }

        private void OnLocaleLoaded(AsyncOperationHandle<Locale> operation)
        {
            _pendingLocale = default;
            OnLocaleChanged(operation.Result);
        }

        private void OnLocaleFontChanged(TMP_FontAsset font)
        {
            var locale = _LocaleFont.LocaleOverride != null ? _LocaleFont.LocaleOverride : LocalizationSettings.SelectedLocale;
            Apply(locale, font);
        }

        private void OnLocaleChanged(Locale locale)
        {
            // No locale in edit mode means the Scene Controls preview is off and driven values are reverted.
            if (locale == null && !Application.isPlaying)
            {
                Clear();
                NotifyChanged();
                return;
            }

            if (_LocaleFont.IsEmpty)
                Apply(locale, null);
        }

        private void Apply(Locale locale, TMP_FontAsset localeFont)
        {
            if (locale == null && !Application.isPlaying)
                return;

            var font = localeFont != null ? localeFont : _DefaultFont;
            if (font == null)
                return;

            CurrentFont = font;
            CurrentMaterial = GetMaterial(font);
            CurrentOverride = GetOverride(locale);
            NotifyChanged();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _preview = null;
            if (_changed == null)
                return;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || _changed == null)
                    return;

                Unsubscribe();
                Subscribe();
            };
        }

        public TMP_FontAsset GetFontInEditor(Locale locale)
        {
            if (locale == null || _LocaleFont.IsEmpty)
                return _DefaultFont;

            var reference = new LocalizedTmpFont
            {
                TableReference = _LocaleFont.TableReference,
                TableEntryReference = _LocaleFont.TableEntryReference,
                LocaleOverride = locale
            };
            var font = reference.LoadAsset();
            return font != null ? font : _DefaultFont;
        }

        public IEnumerable<TMP_FontAsset> GetAllFontsInEditor()
        {
            return LocalizationSettings.AvailableLocales.Locales.Select(GetFontInEditor).Where(f => f != null).Distinct();
        }

        public bool IsMatching(TMP_FontAsset font, Material material)
        {
            return font != null && font == _DefaultFont && GetMaterial(font) == material;
        }

        private LocalePreview GetPreview(Locale locale)
        {
            var font = GetFontInEditor(locale);
            return new LocalePreview
            {
                Locale = locale.LocaleName,
                Font = font,
                Material = GetMaterial(font),
                Scale = GetOverride(locale)?.SizeScale ?? 1f
            };
        }

        [UsedImplicitly]
        private void CopyAtlasCharacters()
        {
            if (_DefaultFont == null)
                return;

            var characters = new HashSet<uint>();
            if (_DefaultFont.atlasPopulationMode == AtlasPopulationMode.Static)
                characters.UnionWith(_DefaultFont.characterTable.Select(c => c.unicode));

            foreach (var locale in LocalizationSettings.AvailableLocales.Locales.Where(l => GetFontInEditor(l) == _DefaultFont))
                characters.UnionWith(FontGlyphCoverage.GetLocaleCharacters(locale.Identifier));

            FontGlyphCoverage.AddCharacters(_ExtraCharacters, characters);
            UnityEditor.EditorGUIUtility.systemCopyBuffer = FontGlyphCoverage.ToText(characters);
            Debug.Log($"Copied {characters.Count} characters for {_DefaultFont.name}", this);
        }

        [UsedImplicitly]
        private void ValidateExtraCharacters(string value, SelfValidationResult result)
        {
            if (_DefaultFont == null || string.IsNullOrEmpty(value))
                return;

            var characters = new HashSet<uint>();
            FontGlyphCoverage.AddCharacters(value, characters);
            var missing = new List<uint>();
            var dynamic = new List<uint>();
            FontGlyphCoverage.Check(_DefaultFont, characters, missing, dynamic);

            if (missing.Count > 0)
                result.AddError($"{_DefaultFont.name} can't draw {FontGlyphCoverage.Format(missing)}");
            if (dynamic.Count > 0)
                result.AddWarning($"{FontGlyphCoverage.Format(dynamic)} not in {_DefaultFont.name} atlas, rendered at runtime");
        }

        [UsedImplicitly]
        private void ValidateGlyphs(TMP_FontAsset value, SelfValidationResult result)
        {
            if (value == null || !LocalizationSettings.HasSettings)
                return;

            foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
            {
                var font = GetFontInEditor(locale);
                var missing = new List<uint>();
                var dynamic = new List<uint>();
                FontGlyphCoverage.Check(font, FontGlyphCoverage.GetLocaleCharacters(locale.Identifier), missing, dynamic);

                if (missing.Count > 0)
                    result.AddError($"{locale.LocaleName}: {font.name} can't draw {FontGlyphCoverage.Format(missing)}");
                if (dynamic.Count > 0)
                    result.AddWarning($"{locale.LocaleName}: {FontGlyphCoverage.Format(dynamic)} not in {font.name} atlas, rendered at runtime");
            }
        }

        [UsedImplicitly]
        private void ValidateLocaleOverrides(List<LocaleOverride> value, SelfValidationResult result)
        {
            if (value == null)
                return;

            var codes = LocalizationSettings.AvailableLocales.Locales.Select(l => l.Identifier.Code).ToHashSet();
            var seen = new HashSet<string>();
            foreach (var code in value.Where(o => o?.Locales != null).SelectMany(o => o.Locales))
            {
                if (!codes.Contains(code))
                    result.AddWarning($"Unknown locale '{code}' in overrides");
                else if (!seen.Add(code))
                    result.AddWarning($"Locale '{code}' is in multiple overrides");
            }
        }

        [UsedImplicitly]
        private IEnumerable GetAllFontMaterials()
        {
            if (_DefaultFont == null)
                return null;

            // Same lookup as TMP's material preset dropdown, only materials named after the font.
            var atlas = _DefaultFont.atlasTexture;
            return UnityEditor.AssetDatabase.FindAssets($"t:Material {_DefaultFont.name.Split(' ')[0]}", new[] {"Assets"})
                .Select(guid => UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid)))
                .Where(m => m != null && m.HasProperty(ShaderUtilities.ID_MainTex) && m.GetTexture(ShaderUtilities.ID_MainTex) == atlas)
                .Select(m => new ValueDropdownItem<Material>(m.name.StartsWith(_DefaultFont.name) ? m.name.Remove(0, _DefaultFont.name.Length).Trim() : m.name, m));
        }
#endif

        [Serializable]
        public class LocaleOverride
        {
            [SerializeField]
            [ListDrawerSettings(ShowFoldout = false)]
            [LabelText(" ")]
            [ValueDropdown("GetAllLocales", IsUniqueList = true)]
            private List<string> _Locales;

            [SerializeField]
            [VerticalGroup("Values")]
            [LabelWidth(110)]
            [MinValue(0.01)]
            private float _SizeScale = 1f;

            [SerializeField]
            [VerticalGroup("Values")]
            [LabelWidth(110)]
            private float _LineSpacing;

            [SerializeField]
            [VerticalGroup("Values")]
            [LabelWidth(110)]
            private float _CharacterSpacing;

            public List<string> Locales => _Locales;
            public float SizeScale => _SizeScale;
            public float LineSpacing => _LineSpacing;
            public float CharacterSpacing => _CharacterSpacing;

            [UsedImplicitly]
            private IEnumerable GetAllLocales()
            {
                return LocalizationSettings.AvailableLocales.Locales.Select(l => new ValueDropdownItem<string>(l.LocaleName, l.Identifier.Code));
            }
        }

#if UNITY_EDITOR
        private class LocalePreview
        {
            [ShowInInspector]
            [TableColumnWidth(80, false)]
            public string Locale;

            [ShowInInspector]
            public TMP_FontAsset Font;

            [ShowInInspector]
            public Material Material;

            [ShowInInspector]
            [TableColumnWidth(50, false)]
            public float Scale;
        }
#endif
    }
}
