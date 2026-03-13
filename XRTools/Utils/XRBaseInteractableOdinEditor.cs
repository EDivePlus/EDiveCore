// Author: František Holubec
// Created: 09.03.2026

#if UNITY_EDITOR && XR_INTERACTION_TOOLKIT
using EDIVE.OdinExtensions.Editor;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.XRTools.Editor
{
    [CustomEditor(typeof(XRBaseInteractable), true)]
    [CanEditMultipleObjects]
    public class XRBaseInteractableOdinEditor : AutoNativeWrapperOdinEditor<XRBaseInteractable> { }
}
#endif
