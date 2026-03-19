using System.Collections.Generic;
using System.Linq;
using EDIVE.Core.Services;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.XRTools.Controls
{
    public class ControlsManager : AServiceBehaviour<ControlsManager>, ISelfValidator
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

        protected void Awake()
        {
            _currentControls = XRUtils.XREnabled ? _HeadsetControls : _DesktopControls;
            AllControls.ForEach(c => c.SetActive(false));
            _currentControls.SetActive(true);
        }

        [Button]
        public void RequestTeleport(Vector3 position, Quaternion? rotation = null)
        {
            if (_currentControls)
            {
                _currentControls.RequestTeleport(position, rotation);
            }
        }
        
        public void Validate(SelfValidationResult result)
        {
#if UNITY_EDITOR
            var componentsData = GetComponentsInChildren<Component>(true)
                .Select(component =>
                {
                    var serializedObject = new SerializedObject(component);
                    var interactionManagerProperty = serializedObject.FindProperty("m_InteractionManager");
                    return new {component, interactionManagerProperty};
                })
                .Where(x => x.interactionManagerProperty != null && x.interactionManagerProperty.objectReferenceValue == null)
                .ToList();

            if (componentsData.Any())
            {
                result.AddError("Missing Interaction Manager on some components.")
                    .WithFix(() =>
                    {
                        foreach (var componentData in componentsData)
                        {
                            if (componentData.interactionManagerProperty == null) continue;
                            componentData.interactionManagerProperty.objectReferenceValue = InteractionManager;
                            componentData.interactionManagerProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        }
                    });
            }
#endif
        }
    }
}
