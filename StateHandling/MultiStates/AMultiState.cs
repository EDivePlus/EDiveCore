using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.StateHandling.MultiStates
{
    public abstract class AMultiState : MonoBehaviour
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
        [ValidateInput("ValidateDefaultState")]
        [ValueDropdown("GetAllStates")]
        private string _DefaultState;

        [PropertyOrder(-10)]
        [ShowInInspector]
        [InlineButton("RefreshState", "Refresh")]
        [ValueDropdown("GetAllStates")]
        public string State { get => _state; set => SetState(value); }

        public string DefaultState
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

        private string _state;

        public bool SetState(string state, bool immediate = false)
        {
            if (!TrySetStateInternal(state, immediate))
                return true;
            _state = state;
            return false;
        }

        public bool SetState(Enum stateID, bool immediate = false) { return SetState(stateID.ToString(), immediate); }

        protected abstract bool TrySetStateInternal(string state, bool immediate = false);

        public bool HasState(string state) => GetAllStates().Any(s => s == state);

        private void RefreshState() => TrySetStateInternal(State);

        private void Awake()
        {
            if (_SetDefaultStateOnAwake)
                SetState(DefaultState);
        }

        public abstract IEnumerable<string> GetAllStates();

#if UNITY_EDITOR
        public abstract void AddState(string id);
        public abstract bool RemoveState(string id);
        
        [UsedImplicitly]
        private bool ValidateDefaultState(string value, ref string errorMessage, ref InfoMessageType? messageType)
        {
            if (GetAllStates().All(v => v != value))
            {
                errorMessage = "Default state is invalid!";
                messageType = InfoMessageType.Error;
                return false;
            }

            return true;
        }
#endif
    }
}
