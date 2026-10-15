using System;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using UnityEngine;

namespace EDIVE.UIElements.Tabs
{
    [System.Obsolete("Replaced by ViewSwitch pages, see EDIVE.View.Windows.ViewSwitch")]
    public class TabHandler : MonoBehaviour
    {
        [SerializeReference]
        private IActivation _Activation;

        [SerializeField]
        private AToggleState _Toggle;
        
        [SerializeField]
        private GameObject _Panel;

        public event Action<TabHandler> Selected;

        private void OnEnable()
        {
            _Activation?.Register(OnButtonClicked);
        }

        private void OnDisable()
        {
            _Activation?.Unregister(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            Selected?.Invoke(this);
        }

        public void SetActive(bool state)
        {
            if (_Toggle) 
                _Toggle.SetState(state);
            
            if (_Panel)
                _Panel.SetActive(state);
        }
    }
}
