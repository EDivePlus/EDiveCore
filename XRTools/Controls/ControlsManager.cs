using EDIVE.Core.Services;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace EDIVE.XRTools.Controls
{
    public class ControlsManager : AServiceBehaviour<ControlsManager>
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

        public XRInteractionManager InteractionManager => _InteractionManager;
        public TeleportationProvider TeleportationProvider => _TeleportationProvider;

        public ControlsHandler Controls => XRUtils.XREnabled ? _HeadsetControls : _DesktopControls;

        protected override void Awake()
        {
            base.Awake();
            _DesktopControls.gameObject.SetActive(!XRUtils.XREnabled);
            _HeadsetControls.gameObject.SetActive(XRUtils.XREnabled);
        }
    }
}
