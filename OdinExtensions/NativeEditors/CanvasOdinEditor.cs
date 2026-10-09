// Author: František Holubec
// Created: 09.10.2026

using System;
using EDIVE.EditorUtils.EditorPriority;
using EDIVE.OdinExtensions.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.NativeEditors
{
    [CustomEditor(typeof(Canvas))]
    [EditorPriority]
    [CanEditMultipleObjects]
    public class CanvasOdinEditor : NativeWrapperOdinEditor<Canvas>
    {
        protected override Type BaseEditorType => typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.CanvasEditor");
    }
}
