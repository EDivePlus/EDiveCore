using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StateHandling.MultiStates
{
    [System.Serializable]
    public class MultiStateRecord
    {
        [SerializeField]
        private string _StateID;
        
        [HideLabel]
        [SerializeField]
        [InlineProperty]
        private ObjectStatePresetField _ObjectPresets = new();

        public string StateID
        {
            get => _StateID;
            set => _StateID = value;
        }

        public IReadOnlyList<ObjectStatePresetRecord> ObjectPresets => _ObjectPresets.ObjectPresets;

        public MultiStateRecord() { }
        public MultiStateRecord(string stateID)
        {
            _StateID = stateID;
        }
        
        public MultiStateRecord(string stateID, List<ObjectStatePresetRecord> objectPresets)
        {
            _StateID = stateID;
            _ObjectPresets = new ObjectStatePresetField(objectPresets);
        }
        
        public void Apply()
        {
            _ObjectPresets.Apply();
        }
    }
}
