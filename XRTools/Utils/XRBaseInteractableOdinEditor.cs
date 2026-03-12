// Author: František Holubec
// Created: 09.03.2026

#if UNITY_EDITOR
using EDIVE.OdinExtensions.Editor;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.XRTools
{
    [CustomEditor(typeof(XRBaseInteractable), true)]
    public class XRBaseInteractableOdinEditor : AutoNativeWrapperOdinEditor<XRBaseInteractable> { }
}
#endif
