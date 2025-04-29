using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StateHandling.MultiStates
{
    public class MultiStateObjects : AMultiState
    {
        [SerializeField]
        [InlineProperty]
        [HideReferenceObjectPicker]
        [CustomValueDrawer("CustomStatePresetDrawer")]
        internal List<GameObjectRecord> _StatePresets = new();

        protected override bool TrySetStateInternal(string stateID, bool immediate = false)
        {
            if (_StatePresets == null)
                return false;

            if (_StatePresets.All(p => p.StateID != stateID))
            {
                Debug.LogError($"[MultiStateBehaviour] No state with ID '{stateID}' at {name}", this);
                return false;
            }

            if (!_StatePresets.TryGetFirst(s => s.StateID == stateID, out var activeStatePreset))
                return false;

            foreach (var statePreset in _StatePresets)
            {
                if (statePreset != activeStatePreset)
                    statePreset.SetActive(false);
            }

            activeStatePreset.SetActive(true);
            return true;

        }

        public override IEnumerable<string> GetAllStates()
        {
            return _StatePresets.Select(statePreset => statePreset.StateID);
        }

#if UNITY_EDITOR
        [UsedImplicitly]
        private GameObjectRecord CustomStatePresetDrawer(GameObjectRecord value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
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
