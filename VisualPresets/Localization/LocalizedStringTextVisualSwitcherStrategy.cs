// Author: František Holubec
// Created: 10.11.2025

#if UNITY_LOCALIZATION
using EDIVE.NativeUtils;
using EDIVE.VisualPresets.Switchers;
using EDIVE.VisualPresets.VisualIDs;
using UnityEngine.Localization.Components;
using UnityEngine.Scripting;

namespace EDIVE.VisualPresets.Localization
{
    [Preserve]
    public class LocalizedStringTextVisualSwitcherStrategy : AVisualSwitcherStrategy<StringVisualID, LocalizedStringVisualPresetRecord, StringVisualSwitcherRecord>
    {
        protected override void Apply(LocalizedStringVisualPresetRecord presetRecord, StringVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.Text == null) 
                return;
            
            var localizeStringEvent = switcherRecord.Text.GetOrAddComponent<LocalizeStringEvent>();
            localizeStringEvent.enabled = true;
            localizeStringEvent.StringReference = presetRecord.LocalizedText;
        }

        protected override void CleanUp(StringVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.Text == null)
                return;

            if (switcherRecord.Text.TryGetComponent(out LocalizeStringEvent localizeStringEvent))
            {
                localizeStringEvent.enabled = false;
            }
        }
    }
}
#endif
