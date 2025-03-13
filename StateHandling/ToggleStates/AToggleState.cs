using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StateHandling.ToggleStates
{
    public abstract class AToggleState : MonoBehaviour
    {
        [SerializeField]
        private string _Description;

        protected bool _state;

        [ShowInInspector]
        [InlineButton("UpdateState", "Update State")]
        public bool State => _state;

        public void SetState(bool state, bool immediate = false)
        {
            _state = state;
            UpdateState(immediate);
        }

        public abstract void UpdateState(bool immediate = false);
    }
}
