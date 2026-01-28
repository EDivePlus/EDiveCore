// Author: Michal Petr
// Created: 29.10.2025

#if UNITY_LOCALIZATION
using System;
using EDIVE.Localization;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets.Localization
{
    [Serializable]
    public class LocalizedStringVisualPresetRecord : AVisualPresetRecord<StringVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private SafeLocalizedString _LocalizedText;
        
        public SafeLocalizedString LocalizedText
        {
            get => _LocalizedText;
            set => _LocalizedText = value;
        }
        
        public override string EditorLabel => "Localized String";

        protected bool Equals(LocalizedStringVisualPresetRecord other)
        {
            return Equals(_LocalizedText, other._LocalizedText) && Equals(_VisualID, other._VisualID);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((LocalizedStringVisualPresetRecord) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_LocalizedText, _VisualID);
        }
    }
}
#endif
