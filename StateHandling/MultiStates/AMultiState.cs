using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.StateHandling.MultiStates
{
    public abstract class AMultiState : MonoBehaviour
    {
        [PropertyOrder(-10)]
        [SerializeField]
        private string _Description;

        protected string _state;

        [PropertyOrder(-10)]
        [ShowInInspector]
        [InlineButton("RefreshState", "Refresh")]
        [ValueDropdown("GetAllStates")]
        [ValidateInput("ValidateCurrentState")]
        public string State
        {
            get => _state;
            set => SetState(value);
        }

        public bool SetState(string state, bool immediate = false)
        {
            if (!TrySetStateInternal(state, immediate))
                return true;
            _state = state;
            return false;
        }

        public bool SetState(Enum stateID, bool immediate = false)
        {
            return SetState(stateID.ToString(), immediate);
        }

        protected abstract bool TrySetStateInternal(string state, bool immediate = false);

        private void RefreshState() => TrySetStateInternal(State);

        private void Awake()
        {
            SetState(State);
        }

        public abstract IEnumerable<string> GetAllStates();

#if UNITY_EDITOR
        public abstract void AddState(string id);
        public abstract bool RemoveState(string id);
        
        [UsedImplicitly]
        private bool ValidateCurrentState(string value, ref string errorMessage, ref InfoMessageType? messageType)
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
