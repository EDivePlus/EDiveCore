// Author: František Holubec
// Created: 19.05.2026

using System;
using System.Collections.Generic;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace EDIVE.StagePlay
{
    public class StagePlaySharedData : ScriptableObject
    {
        [SerializeField]
        private List<CharacterData> _Characters;

        [Serializable]
        private class CharacterData
        {
            [SerializeField]
            private List<string> _Names = new();
            
            [SerializeField]
            private Color _Color;
            
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
        
        private bool TryGetCharacterData(string characterName, out CharacterData data)
        {
            return _Characters.TryGetFirst(c => c.Names.Contains(characterName), out data);
        }
        
        public bool TryGetCharacterColor(string characterName, out Color color)
        {
            if (TryGetCharacterData(characterName, out var data))
            {
                color = data.Color;
                return true;
            }

            color = default;
            return false;
        }

#if UNITY_EDITOR
        [Button(DisplayParameters = false)]
        private void GenerateColors(InspectorProperty property)
        {
            if (_Characters == null || _Characters.Count == 0) return;

            var count = _Characters.Count;
            for (var i = 0; i < count; i++)
            {
                var hue = (float) i / count;
                _Characters[i].Color = Color.HSVToRGB(hue, 0.7f, 0.95f);
            }
            property.MarkSerializationRootDirty();
        }
#endif
    }
}
