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
