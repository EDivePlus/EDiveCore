using System;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using UnityEngine;

namespace EDIVE.UIElements.Tabs
{
    public class TabHandler : MonoBehaviour
    {
        [SerializeReference]
        private IActivation _Activation;

        [SerializeField]
        private AToggleState _Toggle;

        public event Action<TabHandler> Selected;

        private void OnEnable()
        {
            _Activation?.RegisterActivationListener(OnButtonClicked);
        }

        private void OnDisable()
        {
            _Activation?.UnregisterActivationListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            Selected?.Invoke(this);
        }

        public void SetActive(bool state)
        {
            if(_Toggle) 
                _Toggle.SetState(state);
        }
    }
}
