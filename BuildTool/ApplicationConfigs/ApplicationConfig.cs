// Author: František Holubec
// Created: 15.10.2025

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EDIVE.BuildTool.BuildSetupData;
using EDIVE.EditorUtils;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using Unity.EditorCoroutines.Editor;
using UnityEditor;

using UnityEngine;

namespace EDIVE.BuildTool.ApplicationConfigs
{
    public class ApplicationConfig : ScriptableObject, IBuildDataProvider
    {
        [SerializeReference]
        [ListDrawerSettings(ShowFoldout = false)]
        [ValueDropdown(nameof(GetComponentsDropdown), IsUniqueList = true, DrawDropdownForListElements = false)]
        private List<AApplicationConfigComponent> _Components = new();
        
        [SerializeField]
        [InlineProperty]
        [HideLabel]
        private MultiPlatformBuildSetupData _BuildSetupData;

        public IEnumerable<string> GetBuildDefines(BuildContext context)
        {
            return _BuildSetupData.GetData(context.PlatformConfig.NamedBuildTarget, context.PlatformConfig.BuildTarget).SelectMany(d => d.Defines);
        }

        public IEnumerable<IBuildCallback> GetBuildCallbacks(BuildContext context)
        {
            return _BuildSetupData.GetData(context.PlatformConfig.NamedBuildTarget, context.PlatformConfig.BuildTarget).SelectMany(d => d.Actions);
        }
        
        public IEnumerator Apply()
        {
            Debug.Log($"[ApplicationConfig] Applying config '{name}'");
            if (_Components == null)
            {
                Debug.Log("[ApplicationConfig] No defined components!");
                yield break;
            }

            var sortedComponents = _Components.OrderBy(c => c.Priority).ToList();
            foreach (var component in sortedComponents)
            {
                Debug.Log($"[ApplicationConfig] Component {component.Label}");
                yield return component.Apply();
                yield return null;
            }

            Debug.Log("[ApplicationConfig] Completed");
            yield return null;
        }

        public IEnumerator LoadCurrent()
        {
            var previousSelection = Selection.objects;
            var wasLocked = ActiveEditorTracker.sharedTracker.isLocked;
            ActiveEditorTracker.sharedTracker.isLocked = true;
            foreach (var component in _Components)
            {
                yield return component.LoadCurrent();
            }
            Selection.objects = previousSelection;
            ActiveEditorTracker.sharedTracker.isLocked = wasLocked;
        }
  
        public bool HasComponent<T>() where T : AApplicationConfigComponent
        {
            return TryGetComponent<T>(out _);
        }

        public bool TryGetComponent<T>(out T result) where T : AApplicationConfigComponent
        {
            return _Components.TryGetFirstT(out result);
        }
        
        private IEnumerable<ValueDropdownItem<AApplicationConfigComponent>> GetComponentsDropdown()
        {
            return TypeCacheUtils.GetAssignableClassesOfType<AApplicationConfigComponent>()
                .OrderBy(c => c.Priority)
                .Select(c => new ValueDropdownItem<AApplicationConfigComponent>(c.Label, c));
        }
        
        [Button]
        [HorizontalGroup("Controls")]
        public void LoadAllCurrent()
        {
            if (EditorUtility.DisplayDialog("Load from current settings?", "Are you sure you want to overwrite this config from current project settings?", "Ok", "Cancel"))
                EditorCoroutineUtility.StartCoroutine(LoadCurrentWithProgress(), this);
        }

        private IEnumerator LoadCurrentWithProgress()
        {
            EditorUtility.DisplayProgressBar("Application Config", "Applying...", 0);
            yield return LoadCurrent();
            EditorUtility.ClearProgressBar();
        }
        
        [Button]
        [HorizontalGroup("Controls")]
        public void ApplyAll()
        {
            if (EditorUtility.DisplayDialog(
                    "Apply preset", 
                    $"Are you sure you want to overwrite current project settings with this preset?{(Application.isPlaying ? "\nThis will exit PlayMode!" : "")}", 
                    "Ok", "Cancel"))
            {
                EditorApplication.isPlaying = false;
                EditorCoroutineUtility.StartCoroutine(ApplyWithProgress(), this);
            }
        }
        
        private IEnumerator ApplyWithProgress()
        {
            EditorUtility.DisplayProgressBar("Application Config", "Applying...", 0);
            yield return Apply();
            EditorUtility.ClearProgressBar();
        }
    }
}
