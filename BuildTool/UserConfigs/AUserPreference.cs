// Author: František Holubec
// Created: 16.09.2026

using System;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.UserConfigs
{
    [Serializable]
    public abstract class AUserPreference
    {
        public virtual string Label => ObjectNames.NicifyVariableName(GetType().Name);
        
        [PropertyOrder(-100)]
        [OnInspectorGUI]
        private void DrawTitle()
        {
            GUILayout.Label(Label, EditorStyles.boldLabel);
        }
    }
}
