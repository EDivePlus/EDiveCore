// Author: František Holubec
// Created: 15.10.2025

using System;
using System.Collections;
using Sirenix.OdinInspector;
using Unity.EditorCoroutines.Editor;
using UnityEditor;

namespace EDIVE.BuildTool.ApplicationConfigs
{
    [Serializable]
    public abstract class AApplicationConfigComponent
    {
        public abstract IEnumerator Apply();
        public abstract IEnumerator LoadCurrent();
        public abstract bool Validate();

        public virtual string Label => ObjectNames.NicifyVariableName(GetType().Name);
        public virtual int Priority => 0;
        
        [PropertyOrder(-10)]
        [Button("Load current")]
        [HorizontalGroup("Controls")]
        private void LoadFromCurrentSettingsInEditor()
        {
            if (EditorUtility.DisplayDialog("Load from current settings?", "Are you sure you want to overwrite this preset from current project settings?", "Ok", "Cancel"))
                EditorCoroutineUtility.StartCoroutine(FromCurrentSettingsCoroutine(), this);
        }
        
        [PropertyOrder(-10)]
        [Button("Apply")]
        [HorizontalGroup("Controls")]
        private void ApplyInEditor()
        {
            if (EditorUtility.DisplayDialog("Apply preset?", "Are you sure you want to overwrite current project settings with this preset?", "Ok", "Cancel"))
                EditorCoroutineUtility.StartCoroutine(Apply(), this);
        }
        
        public IEnumerator FromCurrentSettingsCoroutine()
        {
            var previousSelection = Selection.objects;
            var wasLocked = ActiveEditorTracker.sharedTracker.isLocked;
            ActiveEditorTracker.sharedTracker.isLocked = true;
            yield return LoadCurrent();
            Selection.objects = previousSelection;
            ActiveEditorTracker.sharedTracker.isLocked = wasLocked;
        }
    }
}
