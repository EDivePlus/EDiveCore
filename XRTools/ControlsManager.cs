using EDIVE.Core.Services;
using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.XRTools
{
    public class ControlsManager : AServiceBehaviour<ControlsManager>
    {
        [ShowInInspector]
        public bool XREnabled => UnityEngine.XR.XRSettings.enabled;

        [SerializeField]
        private Transform _Head;

        [SerializeField]
        private Transform _LeftController;

        [SerializeField]
        private Transform _RightController;

        [SerializeField]
        private AToggleState _XREnabledToggle;

        public Transform Head => _Head;
        public Transform LeftController => _LeftController;
        public Transform RightController => _RightController;

        protected override void Awake()
        {
            base.Awake();
            _XREnabledToggle.SetState(UnityEngine.XR.XRSettings.enabled);
        }
    }
}
