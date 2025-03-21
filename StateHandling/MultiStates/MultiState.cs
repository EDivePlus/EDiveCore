using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StateHandling.MultiStates
{
    public class MultiState : AMultiState
    {
        [SerializeField]
        [InlineProperty]
        [HideReferenceObjectPicker]
        [CustomValueDrawer("CustomStatePresetDrawer")]
        internal List<MultiStateRecord> _StatePresets = new();

        public override bool SetState(string stateID)
        {
            base.SetState(stateID);
            if (_StatePresets == null) 
                return false;
 
            foreach (var statePreset in _StatePresets)
            {
                if (statePreset.StateID != stateID) continue;
                statePreset.Apply();
                return true;
            }

            Debug.LogError($"[MultiState] No state with ID '{stateID}'", this);
            return false;
        }

        public override IEnumerable<string> GetAvailableStates()
        {
            return _StatePresets.Select(s => s.StateID);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private MultiStateRecord CustomStatePresetDrawer(MultiStateRecord value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            var previousBgColor = GUI.backgroundColor;
            GUI.backgroundColor = FancyColors.GetRainbowColor(_StatePresets.IndexOf(value), _StatePresets.Count);
            Sirenix.Utilities.Editor.SirenixEditorGUI.BeginBox();
            GUI.backgroundColor = previousBgColor;
            callNextDrawer(label);
            Sirenix.Utilities.Editor.SirenixEditorGUI.EndBox();
            return value;
        }

        public override IEnumerable<string> GetAllStates()
        {
            return _StatePresets.Select(statePreset => statePreset.StateID);
        }

        public override void AddState(string id)
        {
            _StatePresets.Add(new MultiStateRecord(id));
        }

        public override bool RemoveState(string id)
        {
            return _StatePresets.RemoveAll(s => s.StateID == id) > 0;
        }
#endif
    }
}
