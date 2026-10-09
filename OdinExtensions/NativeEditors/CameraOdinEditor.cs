// Author: František Holubec
// Created: 08.10.2026

using System;
using EDIVE.EditorUtils.EditorPriority;
using EDIVE.OdinExtensions.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.NativeEditors
{
    // Wraps the URP camera editor when URP is used
    [CustomEditor(typeof(Camera))]
    [EditorPriority]
    [CanEditMultipleObjects]
    public class CameraOdinEditor : NativeWrapperOdinEditor<Camera>
    {
        private static readonly Type URP_CAMERA_EDITOR = Type.GetType("UnityEditor.Rendering.Universal.UniversalRenderPipelineCameraEditor, Unity.RenderPipelines.Universal.Editor");

        protected override Type BaseEditorType => URP_CAMERA_EDITOR ?? typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.CameraEditor");
    }
}
