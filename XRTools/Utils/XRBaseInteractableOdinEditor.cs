// Author: František Holubec
// Created: 09.03.2026

#if UNITY_EDITOR && XR_INTERACTION_TOOLKIT
using System;
using EDIVE.EditorUtils;
using EDIVE.EditorUtils.EditorPriority;
using EDIVE.OdinExtensions.Editor;
using UnityEditor;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace EDIVE.XRTools.Utils
{
    [CustomEditor(typeof(XRBaseInteractable), true)]
    [EditorPriority]
    [CanEditMultipleObjects]
    public class XRBaseInteractableOdinEditor : AutoNativeWrapperOdinEditor
    {
        private Type _baseType;
        protected override Type BaseType => _baseType ??= GetBaseType(target.GetType());

        private static Type GetBaseType(Type type)
        {
            var targetAssembly = typeof(XRBaseInteractable).Assembly;

            while (type != null && type.Assembly != targetAssembly)
                type = type.BaseType;

            return type ?? typeof(XRBaseInteractable);
        }
    }
}
#endif
