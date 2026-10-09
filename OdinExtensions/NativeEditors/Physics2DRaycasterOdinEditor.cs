// Author: František Holubec
// Created: 09.10.2026

using EDIVE.EditorUtils.EditorPriority;
using EDIVE.OdinExtensions.Editor;
using UnityEditor;
using UnityEditor.EventSystems;
using UnityEngine.EventSystems;

namespace EDIVE.OdinExtensions.NativeEditors
{
    [CustomEditor(typeof(Physics2DRaycaster))]
    [EditorPriority]
    [CanEditMultipleObjects]
    public class Physics2DRaycasterOdinEditor : NativeWrapperOdinEditor<Physics2DRaycaster, Physics2DRaycasterEditor>
    {
    }
}
