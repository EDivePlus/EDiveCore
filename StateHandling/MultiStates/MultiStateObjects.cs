using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.StateHandling.MultiStates
{
    public class MultiStateObjects : AMultiState
    {
        [SerializeField]
        [InlineProperty]
        [HideReferenceObjectPicker]
        [ListDrawerSettings(ShowFoldout = false)]
        internal List<MultiStateObjectsRecord> _StatePresets = new();

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
        public override void AddState(string id)
        {
            _StatePresets.Add(new MultiStateObjectsRecord(id));
        }

        public override bool RemoveState(string id)
        {
            return _StatePresets.RemoveAll(s => s.StateID == id) > 0;
        }
#endif
    }
    
    [Serializable]
    public class MultiStateObjectsRecord
    {
        [EnhancedFoldoutGroup("State", "GetFoldoutColor", true)]
        [ShowInFoldoutHeader]
        [HideLabel]
        [SerializeField]
        private string _StateID;

        [EnhancedFoldoutGroup("State")]
        [ListDrawerSettings(ShowFoldout = false)]
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

        public MultiStateObjectsRecord() { }
        public MultiStateObjectsRecord(string stateID, List<GameObject> targets = null)
        {
            _StateID = stateID;
            _Targets = targets ?? new List<GameObject>();
        }

#if UNITY_EDITOR
        private Color GetFoldoutColor(InspectorProperty property)
        {
            var element = property.FindParent(p => p.Parent?.ChildResolver is IOrderedCollectionResolver, true);
            return element != null ? ColorTools.GetRainbowColor(element.Index, element.Parent.Children.Count) : Color.gray;
        }
#endif
    }
}
