using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StateHandling.ToggleStates
{
    public abstract class AToggleState : MonoBehaviour
    {
        [PropertyOrder(-10)]
        [SerializeField]
        private string _Description;

        protected bool _state;

        [PropertyOrder(-10)]
        [ShowInInspector]
        public bool State
        {
            get => _state;
            set => SetState(value);
        }

        [PropertyOrder(-10)]
        [ShowInInspector]
        public bool StateImmediate
        {
            get => _state;
            set => SetState(value, true);
        }

        public void SetState(bool state, bool immediate = false)
        {
            _state = state;
            UpdateState(immediate);
        }

        public abstract void UpdateState(bool immediate = false);
    }
}
