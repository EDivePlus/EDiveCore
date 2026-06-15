// Author: Michal Petr
// Created: 29.10.2025

#if UNITY_LOCALIZATION
using System;
using EDIVE.Localization;
using EDIVE.Utils.Json.TypeNames;
using EDIVE.VisualPresets.Presets;
using EDIVE.VisualPresets.VisualIDs;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace EDIVE.VisualPresets.Localization
{
    [Serializable]
    [JsonObject(MemberSerialization.OptIn)]
    [JsonTypeName("VisualPreset.LocalizedString")]
    [MovedFrom(true, "EDIVE.VisualPresets.Localization", "EDIVE.Localization")]
    public class LocalizedStringVisualPresetRecord : AVisualPresetRecord<StringVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        [JsonProperty("Text")]
        private SafeLocalizedString _LocalizedText;
        
        public SafeLocalizedString LocalizedText
        {
            get => _LocalizedText;
            set => _LocalizedText = value;
        }
        
        public override string EditorLabel => "Localized String";

        [JsonConstructor]
        public LocalizedStringVisualPresetRecord() { }
        public LocalizedStringVisualPresetRecord(StringVisualID visualID, SafeLocalizedString localizedText) : base(visualID) { _LocalizedText = localizedText; }

        public override bool EqualsInternal(AVisualPresetRecord other)
        {
            return other is LocalizedStringVisualPresetRecord localizedPreset && Equals(LocalizedText, localizedPreset.LocalizedText);
        }

        public override int GetHashCodeInternal()
        {
            return _LocalizedText?.GetHashCode() ?? 0;
        }
    }
}
#endif
