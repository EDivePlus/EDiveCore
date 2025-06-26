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
        private GameObject _DesktopControls;

        [SerializeField]
        private GameObject _HeadsetControls;

        [PropertySpace]
        [SerializeField]
        private XRInteractionManager _InteractionManager;

        [SerializeField]
        private TeleportationProvider _TeleportationProvider;

        public XRInteractionManager InteractionManager => _InteractionManager;
        public TeleportationProvider TeleportationProvider => _TeleportationProvider;

        protected override void Awake()
        {
            base.Awake();
            if (_DesktopControls)
                _DesktopControls.SetActive(!XRUtils.XREnabled);
            if (_HeadsetControls)
                _HeadsetControls.SetActive(XRUtils.XREnabled);
        }
    }
}
