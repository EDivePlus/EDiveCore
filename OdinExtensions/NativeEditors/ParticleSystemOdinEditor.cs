// Author: František Holubec
// Created: 09.10.2026

using System;
using EDIVE.EditorUtils.EditorPriority;
using EDIVE.OdinExtensions.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.OdinExtensions.NativeEditors
{
    [CustomEditor(typeof(ParticleSystem))]
    [EditorPriority]
    [CanEditMultipleObjects]
    public class ParticleSystemOdinEditor : NativeWrapperOdinEditor<ParticleSystem>
    {
        protected override Type BaseEditorType => typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ParticleSystemInspector");
    }
}
