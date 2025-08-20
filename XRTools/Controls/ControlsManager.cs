using System.Collections.Generic;
using EDIVE.Core.Services;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.XRTools.Controls
{
    public class ControlsManager : AServiceBehaviour<ControlsManager>
    {
        [SerializeField]
        private AControls _DesktopControls;

        [SerializeField]
        private AControls _HeadsetControls;

        [PropertySpace]
        [SerializeField]
        private XRInteractionManager _InteractionManager;

        [SerializeField]
        private TeleportationProvider _TeleportationProvider;

        public XRInteractionManager InteractionManager => _InteractionManager;
        public TeleportationProvider TeleportationProvider => _TeleportationProvider;
        
        private AControls _currentControls;
        private IEnumerable<AControls> AllControls
        {
            get
            {
                yield return _DesktopControls;
                yield return _HeadsetControls;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _currentControls = XRUtils.XREnabled ? _HeadsetControls : _DesktopControls;
            foreach (var controls in AllControls)
            {
                controls.SetActive(controls == _currentControls);
            }
        }

        public void RequestTeleport(Vector3 position, Quaternion rotation)
        {
            if (_currentControls)
            {
                _currentControls.RequestTeleport(position, rotation);
            }
        }

#if UNITY_EDITOR
        [PropertySpace]
        [Button]
        private void AssignInteractionManager()
        {
            foreach (var component in GetComponentsInChildren<Component>())
            {
                var serializedObject = new SerializedObject(component);
                var interactionManagerProperty = serializedObject.FindProperty("m_InteractionManager");
                if (interactionManagerProperty == null)
                    continue;
                interactionManagerProperty.objectReferenceValue = InteractionManager;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }
#endif
    }
}
