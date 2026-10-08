using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.Utils.DrivenValues;
#endif

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

#if UNITY_EDITOR
        public override void PopulateDrivenValues(DrivenValuesCollection values)
        {
            foreach (var statePreset in _ObjectToggleStatePreset)
            {
                statePreset?.PopulateDrivenValues(values);
            }
        }
#endif
    }
}
