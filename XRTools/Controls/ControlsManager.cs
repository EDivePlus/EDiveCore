using EDIVE.Core.Services;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.XRTools.Controls
{
    public class ControlsManager : AServiceBehaviour<ControlsManager>
    {
        [SerializeField]
        private ControlsHandler _DesktopControls;

        [SerializeField]
        private ControlsHandler _HeadsetControls;

        [ShowInInspector]
        public bool XREnabled => UnityEngine.XR.XRSettings.enabled;

        public ControlsHandler Controls => XREnabled ? _HeadsetControls : _DesktopControls;

        protected override void Awake()
        {
            base.Awake();
            _DesktopControls.gameObject.SetActive(!XREnabled);
            _HeadsetControls.gameObject.SetActive(XREnabled);
        }
    }
}
