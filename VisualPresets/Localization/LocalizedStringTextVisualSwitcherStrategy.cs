// Author: František Holubec
// Created: 10.11.2025

#if UNITY_LOCALIZATION
using System;
using EDIVE.NativeUtils;
using EDIVE.VisualPresets.Switchers;
using EDIVE.VisualPresets.VisualIDs;
using UnityEngine.Localization.Components;
using UnityEngine.Scripting;

namespace EDIVE.VisualPresets.Localization
{
    [Preserve]
    public class LocalizedStringTextVisualSwitcherStrategy : AVisualSwitcherStrategy<StringVisualID, LocalizedStringVisualPresetRecord, TMPTextStringVisualSwitcherRecord>
    {
        protected override IDisposable Apply(LocalizedStringVisualPresetRecord presetRecord, TMPTextStringVisualSwitcherRecord switcherRecord)
        {
            if (switcherRecord.Text == null) 
                return DisposableUtils.Empty;
            
            // Shown right away when the table is loaded, otherwise blank until it is, never the old text
            if (presetRecord.LocalizedText != null && !presetRecord.LocalizedText.IsEmpty)
            {
                var current = presetRecord.LocalizedText.GetLocalizedStringAsync();
                switcherRecord.Text.text = current.IsDone ? current.Result : string.Empty;
            }

            var localizeStringEvent = switcherRecord.Text.GetOrAddComponent<LocalizeStringEvent>();
            localizeStringEvent.OnUpdateString.AddListener(UpdateString);
            localizeStringEvent.enabled = true;
            localizeStringEvent.StringReference = presetRecord.LocalizedText;
            
            return DisposableUtils.Create(() =>
            {
                if (localizeStringEvent == null)
                    return;
                
                localizeStringEvent.OnUpdateString.RemoveListener(UpdateString);
                localizeStringEvent.enabled = false;
            });
            
            void UpdateString(string s) => switcherRecord.Text.text = s;
        }

        protected override void Prepare(TMPTextStringVisualSwitcherRecord switcherRecord)
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
