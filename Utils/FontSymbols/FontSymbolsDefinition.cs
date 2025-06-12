// Author: František Holubec
// Created: 10.06.2025

using System.Linq;
using Sirenix.OdinInspector;
using TMPro;
using EDIVE.Extensions.Random;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

#if UNITY_EDITOR
using UnityEditor;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.Utils.FontSymbols
{
    public class FontSymbolsDefinition : ScriptableObject
    {
        [Required]
        [SerializeField]
        private CodepointsAsset _Codepoints;

        [Required]
        [SerializeField]
        private Font _Font;

        [SerializeField]
        private TMP_FontAsset _TMPFont;

        public CodepointsAsset Codepoints => _Codepoints;
        public Font Font => _Font;
        public TMP_FontAsset TMPFont => _TMPFont;

        public FontSymbolAvailability CheckSymbolAvailability(char character)
        {
            if (!_TMPFont)
                return FontSymbolAvailability.NotSupported;

            uint unicode = character;

            // Check if already present in atlas
            if (_TMPFont.characterLookupTable.ContainsKey(unicode))
                return FontSymbolAvailability.PresentInAtlas;

            // Check if dynamic addition is possible
            if (_TMPFont.atlasPopulationMode == AtlasPopulationMode.Dynamic && _TMPFont.sourceFontFile)
            {
                FontEngine.InitializeFontEngine();
                if (FontEngine.LoadFontFace(_TMPFont.sourceFontFile, 0) == FontEngineError.Success && FontEngine.TryGetGlyphIndex(unicode, out _))
                    return FontSymbolAvailability.DynamicAvailable;
            }

            // Character is not present and can't be added
            return FontSymbolAvailability.NotSupported;
        }

#if UNITY_EDITOR
        [PropertySpace]
        [OnInspectorGUI]
        private void PreviewCharacters()
        {
            if (!_Font) return;
            var random = new SystemRandom(632);
            var text = string.Join("",Codepoints.Entries.RandomItemEnumerate(random).Take(30).Select(p => p.Char));
            var imageStyle = new GUIStyle(SirenixGUIStyles.CenteredTextField)
            {
                font = _Font,
                fontSize = 30,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            EditorGUILayout.LabelField(text, imageStyle);
        }

        [Button]
        private void BrowseCatalog()
        {
            FontSymbolSelectionWindow.Open(this);
        }

        public bool TryAddCharacterToTMP(char symbol)
        {
            var setToStatic = false;
            if (_TMPFont.atlasPopulationMode == AtlasPopulationMode.Static)
            {
                setToStatic = true;
                _TMPFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            }

            var result = _TMPFont.TryAddCharacters(new []{(uint) symbol}, out var missingUnicodes);
            if (result)
            {
                EditorUtility.SetDirty(_TMPFont);
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogWarning($"Failed to parse unicode range for \n{string.Join(",", missingUnicodes)}");
            }

            if (setToStatic)
            {
                _TMPFont.atlasPopulationMode = AtlasPopulationMode.Static;
            }
            return result;
        }

        public void SetStaticTMPCharacters(uint[] codepoints)
        {
            if (!_TMPFont)
            {
                Debug.LogError("TMP Font is not assigned.");
                return;
            }

            _TMPFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            _TMPFont.ClearFontAssetData();
            if (_TMPFont.TryAddCharacters(codepoints, out var missingUnicodes))
            {
                AssetDatabase.SaveAssets();
            }
            else
            {
                Debug.LogWarning($"Failed to parse unicode range for \n{string.Join(",", missingUnicodes)}");
            }

            _TMPFont.atlasPopulationMode = AtlasPopulationMode.Static;
        }
#endif
    }
}
