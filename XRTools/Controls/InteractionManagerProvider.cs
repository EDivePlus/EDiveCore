// Author: František Holubec
// Created: 07.05.2026

using System.Linq;
using EDIVE.Core.Services;
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
    public class InteractionManagerProvider : AServiceBehaviour<InteractionManagerProvider>
    {
        [PropertySpace]
        [SerializeField]
        [EnhancedValidate("ValidateInteractionManager", ContinuousValidationCheck = true)]
        private XRInteractionManager _InteractionManager;

        [SerializeField]
        private TeleportationProvider _TeleportationProvider;

        public XRInteractionManager InteractionManager => _InteractionManager;
        public TeleportationProvider TeleportationProvider => _TeleportationProvider;
        
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
