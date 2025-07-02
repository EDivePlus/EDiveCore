using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.StateHandling.ToggleStates
{
    public abstract class AToggleState : MonoBehaviour
    {
        [PropertyOrder(-10)]
        [SerializeField]
        private string _Description;

        [PropertyOrder(-10)]
        [SerializeField]
        private bool _SetDefaultStateOnAwake;

        [PropertyOrder(-10)]
        [SerializeField]
        [ShowIf(nameof(_SetDefaultStateOnAwake))]
        private bool _DefaultState;

        [PropertyOrder(-10)]
        [ShowInInspector]
        [InlineIconButton("Refresh", "RefreshState")]
        public bool State
        {
            get => _state;
            set => SetState(value);
        }

        public bool DefaultState
        {
            get => _DefaultState;
            set
            {
                _DefaultState = value;
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        private bool _state;

        public void SetState(bool state, bool immediate = false)
        {
            _state = state;
            SetStateInternal(state, immediate);
        }

        protected abstract void SetStateInternal(bool state, bool immediate = false);

        public void RefreshState() => SetStateInternal(State);

        private void Awake()
        {
            if(_SetDefaultStateOnAwake)
                SetState(DefaultState);
        }
    }
}
