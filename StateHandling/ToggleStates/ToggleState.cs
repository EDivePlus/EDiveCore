using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StateHandling.ToggleStates
{
    public class ToggleState : AToggleState
    {
        [PropertySpace(4)]
        [SerializeField]
        [HideReferenceObjectPicker]
        internal List<ToggleStateRecord> _ObjectToggleStatePreset = new();

        protected override void SetStateInternal(bool state, bool immediate = false)
        {
            if (_ObjectToggleStatePreset == null)
                return;

            foreach (var statePreset in _ObjectToggleStatePreset)
            {
                statePreset?.SetState(state);
            }
        }
    }
}
