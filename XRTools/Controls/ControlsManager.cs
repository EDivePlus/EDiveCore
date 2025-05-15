using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading;
using EDIVE.Core.Services;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace EDIVE.XRTools.Controls
{
    public class ControlsManager : ALoadableServiceBehaviour<ControlsManager>
    {
        [SerializeField]
        private ControlsHandler _DesktopControls;

        [SerializeField]
        private ControlsHandler _HeadsetControls;

        [PropertySpace]
        [SerializeField]
        private XRInteractionManager _InteractionManager;

        [SerializeField]
        private TeleportationProvider _TeleportationProvider;

        [ShowInInspector]
        public bool XREnabled => UnityEngine.XR.XRSettings.enabled || XRDeviceSimulator.instance || XRInteractionSimulator.instance;

        public XRInteractionManager InteractionManager => _InteractionManager;
        public TeleportationProvider TeleportationProvider => _TeleportationProvider;

        public ControlsHandler Controls => XREnabled ? _HeadsetControls : _DesktopControls;
        
        

        protected override async UniTask LoadRoutine(Action<float> progressCallback)
        {
            await UniTask.Yield();
            _DesktopControls.gameObject.SetActive(!XREnabled);
            _HeadsetControls.gameObject.SetActive(XREnabled);
        }
    }
    
}
