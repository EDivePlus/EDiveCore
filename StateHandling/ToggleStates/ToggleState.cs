using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StateHandling.ToggleStates
{
    public class ToggleState : AToggleState
    {
        [SerializeField]
        [HideReferenceObjectPicker]
        internal List<ToggleStateRecord> _ObjectToggleStatePreset = new List<ToggleStateRecord>();
        
        public override void UpdateState(bool immediate = false)
        {
            if (_ObjectToggleStatePreset == null) return;

            foreach (var statePreset in _ObjectToggleStatePreset)
            {
                statePreset?.SetState(_state);
            }
        }
    }
}
