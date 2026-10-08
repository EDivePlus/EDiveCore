using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.Utils.DrivenValues;
#endif

namespace EDIVE.StateHandling.ToggleStates
{
    public class ToggleStateObjects : AToggleState
    {
        [VerticalGroup("Targets", PaddingTop = 4)]
        [HorizontalGroup("Targets/Horizontal")]
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        internal List<GameObject> _OnTargets = new();

        [HorizontalGroup("Targets/Horizontal")]
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        internal List<GameObject> _OffTargets = new();

        protected override void SetStateInternal(bool state, bool immediate = false)
        {
            SetTargetsActive(!State, false);
            SetTargetsActive(State, true);
        }

        private void SetTargetsActive(bool state, bool active)
        {
            var targets = state ? _OnTargets : _OffTargets;
            foreach (var target in targets)
            {
                if (target == null) continue;
                target.SetActive(active);
            }
        }

#if UNITY_EDITOR
        public override void PopulateDrivenValues(DrivenValuesCollection values)
        {
            foreach (var target in _OnTargets)
                values.Add(target, "Active");
            foreach (var target in _OffTargets)
                values.Add(target, "Active");
        }
#endif
    }
}
