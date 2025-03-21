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
    public class MultiStateActiveObjects : AMultiState
    {
        [SerializeField]
        [InlineProperty]
        [HideReferenceObjectPicker]
        [CustomValueDrawer("CustomStatePresetDrawer")]
        internal List<GameObjectRecord> _StatePresets = new();

        public override bool SetState(string stateID)
        {
            base.SetState(stateID);
            if (_StatePresets == null) return false;
            if (_StatePresets.All(p => p.StateID != stateID))
            {
                Debug.LogError($"[MultiStateBehaviour] No state with ID '{stateID}' at {name}", this);
                return false;
            }

            foreach (var statePreset in _StatePresets)
            {
                statePreset.SetActive(false);
            }
            foreach (var statePreset in _StatePresets)
            {
                if (statePreset.StateID != stateID) continue;
                statePreset.SetActive(true);
                return true;
            }
            return false;
        }

        public override IEnumerable<string> GetAvailableStates()
        {
            return _StatePresets.Select(s => s.StateID);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private GameObjectRecord CustomStatePresetDrawer(GameObjectRecord value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
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
            _StatePresets.Add(new GameObjectRecord(id));
        }

        public override bool RemoveState(string id)
        {
            return _StatePresets.RemoveAll(s => s.StateID == id) > 0;
        }
#endif
    }
    
    [Serializable]
    public class GameObjectRecord
    {
        [SerializeField]
        private string _StateID;

        [SerializeField]
        private List<GameObject> _Targets = new();

        public string StateID => _StateID;
        public List<GameObject> Targets => _Targets;

        public void SetActive(bool active)
        {
            foreach (var target in _Targets)
            {
                if(target != null) target.SetActive(active);
            }
        }

        public GameObjectRecord() { }
        public GameObjectRecord(string stateID, List<GameObject> targets = null)
        {
            _StateID = stateID;
            _Targets = targets ?? new List<GameObject>();
        }
    }
}
