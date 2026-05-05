using System.Collections.Generic;
using System.Linq;
using EDIVE.Core.Services;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
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
        private AControls _MobileControls;

        [SerializeField]
        private AControls _HeadsetControls;

        [PropertySpace]
        [SerializeField]
        [EnhancedValidate("ValidateInteractionManager", ContinuousValidationCheck = true)]
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
                yield return _MobileControls;
                yield return _HeadsetControls;
            }
        }

        protected void Awake()
        {
            _currentControls = SelectControls();
            AllControls.Where(c => c != null).ForEach(c => c.SetActive(false));
            if (_currentControls != null)
                _currentControls.SetActive(true);
        }

        private AControls SelectControls()
        {
            if (XRUtils.XREnabled)
                return _HeadsetControls;
            if (Application.isMobilePlatform)
                return _MobileControls;
            return _DesktopControls;
        }

        [Button]
        public void RequestTeleport(Vector3 position, Quaternion? rotation = null)
        {
            if (_currentControls)
            {
                _currentControls.RequestTeleport(position, rotation);
            }
        }
       
#if UNITY_EDITOR
        [UsedImplicitly]
        public void ValidateInteractionManager(XRInteractionManager value, SelfValidationResult result)
        {
            if (value == null)
                return;
            
            var componentsData = value.GetComponentsInChildren<Component>(true)
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
        }
#endif
    }
}
