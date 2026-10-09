// Author: František Holubec
// Created: 09.10.2026

using EDIVE.EditorUtils.EditorPriority;
using EDIVE.OdinExtensions.Editor;
using UnityEditor;
using UnityEditor.EventSystems;
using UnityEngine.EventSystems;

namespace EDIVE.OdinExtensions.NativeEditors
{
    [CustomEditor(typeof(PhysicsRaycaster))]
    [EditorPriority]
    [CanEditMultipleObjects]
    public class PhysicsRaycasterOdinEditor : NativeWrapperOdinEditor<PhysicsRaycaster, PhysicsRaycasterEditor>
    {
    }
}
