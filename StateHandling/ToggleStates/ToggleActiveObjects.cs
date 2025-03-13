using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StateHandling.ToggleStates
{
    public class ToggleActiveObjects : AToggleState
    {
        [PropertySpace(4)]
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        internal List<GameObject> _OnTargets = new();

        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        internal List<GameObject> _OffTargets = new();

        public override void UpdateState(bool immediate = false)
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
    }
}
