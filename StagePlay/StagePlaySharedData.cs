// Author: František Holubec
// Created: 19.05.2026

using System;
using System.Collections.Generic;
using System.Text;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StagePlay
{
    public class StagePlaySharedData : ScriptableObject
    {
        [Serializable]
        public class CharacterData
        {
            [SerializeField]
            private List<string> _Names = new();

            [SerializeField]
            private Color _Color = Color.white;

            public List<string> Names
            {
                get => _Names;
                set => _Names = value;
            }

            public Color Color
            {
                get => _Color;
                set => _Color = value;
            }
        }

        private static readonly HashSet<char> NAME_SEPARATORS = new() {',', ';', '.', '、'};

        [SerializeField]
        private List<CharacterData> _Characters = new();

        private Dictionary<string, Color> _colorLookup;

        public IReadOnlyList<CharacterData> Characters => _Characters;

        public bool TryGetCharacterColor(string characterName, out Color color)
        {
            if (string.IsNullOrEmpty(characterName))
            {
                color = default;
                return false;
            }
            return GetColorLookup().TryGetValue(characterName.Trim(), out color);
        }

        public string ColorizeCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            var sb = new StringBuilder(text.Length + 32);
            var tokenStart = 0;
            for (var i = 0; i <= text.Length; i++)
            {
                var atEnd = i == text.Length;
                var isSeparator = !atEnd && NAME_SEPARATORS.Contains(text[i]);
                if (!atEnd && !isSeparator)
                    continue;

                if (i > tokenStart)
                    AppendColoredToken(sb, text, tokenStart, i);

                if (!atEnd)
                    sb.Append(text[i]);

                tokenStart = i + 1;
            }
            return sb.ToString();
        }

        private void AppendColoredToken(StringBuilder sb, string source, int start, int end)
        {
            var leading = start;
            while (leading < end && char.IsWhiteSpace(source[leading]))
                leading++;
            var trailing = end;
            while (trailing > leading && char.IsWhiteSpace(source[trailing - 1]))
                trailing--;

            if (leading == trailing)
            {
                sb.Append(source, start, end - start);
                return;
            }

            var charName = source.Substring(leading, trailing - leading);
            if (!GetColorLookup().TryGetValue(charName, out var color))
            {
                sb.Append(source, start, end - start);
                return;
            }

            if (leading > start)
                sb.Append(source, start, leading - start);
            
            sb.Append(charName.Color(color));
            if (end > trailing)
                sb.Append(source, trailing, end - trailing);
        }
        
        private Dictionary<string, Color> GetColorLookup()
        {
            if (_colorLookup != null)
                return _colorLookup;

            _colorLookup = new Dictionary<string, Color>();
            if (_Characters == null)
                return _colorLookup;

            foreach (var element in _Characters)
            {
                if (element?.Names == null)
                    continue;
                foreach (var charName in element.Names)
                {
                    if (string.IsNullOrEmpty(charName))
                        continue;
                    _colorLookup[charName.Trim()] = element.Color;
                }
            }
            return _colorLookup;
        }

        private void OnValidate() => _colorLookup = null;

#if UNITY_EDITOR
        [Button(DisplayParameters = false)]
        private void GenerateColors()
        {
            if (_Characters == null || _Characters.Count == 0)
                return;

            for (var i = 0; i < _Characters.Count; i++)
                _Characters[i].Color = Color.HSVToRGB((float)i / _Characters.Count, 0.7f, 0.95f);
            _colorLookup = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
