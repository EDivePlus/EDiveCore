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
        [SerializeField]
        [HideInInspector]
        private string _CurrentState;

        [PropertyOrder(-1)]
        [PropertySpace(SpaceBefore = 0, SpaceAfter = 8)]
        [ShowInInspector]
        [ValueDropdown("GetAllStates")]
        [ValidateInput("ValidateCurrentState")]
        [HorizontalGroup("State")]
        public string CurrentState
        {
            get => _CurrentState;
            private set
            {
                _CurrentState = value;
                SetState(_CurrentState);
            }
        }

        private void Awake()
        {
            SetState(_CurrentState);
        }

        public virtual bool SetState(string stateID)
        {
            _CurrentState = stateID;
            return true;
        }

        public bool SetState(Enum stateID)
        {
            return SetState(stateID.ToString());
        }

        public abstract IEnumerable<string> GetAvailableStates();

#if UNITY_EDITOR
        public abstract IEnumerable<string> GetAllStates();
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
        
        [Button("Refresh")]
        [HorizontalGroup("State", 60)]
        private void RefreshCurrentState()
        {
            SetState(_CurrentState);
        }
#endif
    }
}
