// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets.Presets
{
    [Serializable]
    public class StringVisualPresetRecord : AVisualPresetRecord<StringVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private string _Text;
        
        public string Text => _Text;

        public override string EditorLabel => "String";

        public override bool Equals(AVisualPresetRecord other)
        {
            if (other is not StringVisualPresetRecord tOther)
                return false;
            return base.Equals(other) && Text == tOther.Text;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), Text);
        }
    }
}
