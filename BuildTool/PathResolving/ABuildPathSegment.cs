// Author: František Holubec
// Created: 21.03.2025

using System;
using EDIVE.BuildTool.Presets;
using Sirenix.OdinInspector;
using UnityEditor;

namespace EDIVE.BuildTool.PathResolving
{
    [Serializable]
    public abstract class ABuildPathSegment
    {
        public abstract string GetValue(ABuildPreset preset);

        public virtual string Label => ObjectNames.NicifyVariableName(GetType().Name.Replace("PathSegment", ""));
        protected virtual bool HideLabel => false;

        [OnInspectorGUI]
        [HideIf(nameof(HideLabel))]
        [PropertyOrder(-100)]
        private void DrawLabel()
        {
            EditorGUILayout.LabelField($"{Label}");
        }
    }
}
