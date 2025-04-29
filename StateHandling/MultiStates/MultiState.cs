using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
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

        protected override bool TrySetStateInternal(string stateID, bool immediate = false)
        {
            if (_StatePresets == null)
                return false;

            if (!_StatePresets.TryGetFirst(s => s.StateID == stateID, out var statePreset))
            {
                Debug.LogError($"[MultiState] No state with ID '{stateID}'", this);
                return false;
            }

            statePreset.Apply();
            return true;
        }

        public override IEnumerable<string> GetAllStates()
        {
            return _StatePresets.Select(statePreset => statePreset.StateID);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private MultiStateRecord CustomStatePresetDrawer(MultiStateRecord value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            var previousBgColor = GUI.backgroundColor;
            GUI.backgroundColor = ColorTools.GetRainbowColor(_StatePresets.IndexOf(value), _StatePresets.Count);
            Sirenix.Utilities.Editor.SirenixEditorGUI.BeginBox();
            GUI.backgroundColor = previousBgColor;
            callNextDrawer(label);
            Sirenix.Utilities.Editor.SirenixEditorGUI.EndBox();
            return value;
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
