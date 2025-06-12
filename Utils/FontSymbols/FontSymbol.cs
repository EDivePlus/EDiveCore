// Author: František Holubec
// Created: 11.06.2025

using System;
using JetBrains.Annotations;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace EDIVE.Utils.FontSymbols
{
    [Serializable]
    public struct FontSymbol
    {
        [SerializeField]
        private FontSymbolsDefinition _Definition;

        [SerializeField]
        private char _Symbol;

        public FontSymbolsDefinition Definition => _Definition;
        public char Symbol => _Symbol;

        public FontSymbol(FontSymbolsDefinition definition, char symbol)
        {
            _Definition = definition;
            _Symbol = symbol;
        }

        public FontSymbol WithDefinition(FontSymbolsDefinition definition)
        {
            return new FontSymbol(definition, _Symbol);
        }

        public FontSymbol WithSymbol(char symbol)
        {
            return new FontSymbol(_Definition, symbol);
        }

        public FontSymbolAvailability CheckSymbolAvailability()
        {
            return _Definition.CheckSymbolAvailability(Symbol);
        }
    }

#if UNITY_EDITOR
    [UsedImplicitly]
    public sealed class FontSymbolDrawer : OdinValueDrawer<FontSymbol>
    {
        private GUIStyle _imageStyle;

        protected override void Initialize()
        {
            base.Initialize();
            _imageStyle = new GUIStyle(SirenixGUIStyles.Button)
            {
                fontSize = 50,
                alignment = TextAnchor.MiddleCenter,
            };
        }

        protected override void DrawPropertyLayout(GUIContent label)
        {
            var definition = ValueEntry.SmartValue.Definition;
            if (definition && _imageStyle.font != definition.Font)
                _imageStyle.font = definition.Font;

            SirenixEditorGUI.BeginHorizontalPropertyLayout(label);
            GUIContent content = null;
            var symbol = ValueEntry.SmartValue.Symbol;
            if (definition && definition.Codepoints.TryGetCodepoint(ValueEntry.SmartValue.Symbol, out var codepoint))
            {
                content = GUIHelper.TempContent(codepoint.Char.ToString(), $"{codepoint.Name} ({codepoint.HexCode})");
            }
            else
            {
                content = GUIHelper.TempContent("", "No Symbol");
            }
            if (GUILayout.Button(content, _imageStyle, GUILayout.Width(60), GUILayout.Height(60)))
            {
                FontSymbolSelectionWindow.Open(definition, symbol, c =>
                {
                    ValueEntry.SmartValue = new FontSymbol(definition, c.Char);
                    ValueEntry.ApplyChanges();
                    Property.MarkSerializationRootDirty();
                });
            }

            EditorGUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();
            var newDefinition = (FontSymbolsDefinition) SirenixEditorFields.UnityObjectField(definition, typeof(FontSymbolsDefinition), false);
            var newCode = EditorGUILayout.TextField(ConvertCharToHex(symbol));
            if (EditorGUI.EndChangeCheck())
            {
                var newChar = ConvertHexToChar(newCode);
                ValueEntry.SmartValue = new FontSymbol(newDefinition, newChar);
                ValueEntry.ApplyChanges();
                Property.MarkSerializationRootDirty();
            }
            EditorGUILayout.EndVertical();
            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }

        private static string ConvertCharToHex(char code)
        {
            try
            {
                return Convert.ToString(code, 16);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static char ConvertHexToChar(string hex)
        {
            try
            {
                return Convert.ToChar(Convert.ToInt32(hex, 16));
            }
            catch (Exception)
            {
                return '\0';
            }
        }
    }
#endif
}
