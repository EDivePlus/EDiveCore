#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.EditorUtils;
using EDIVE.StateHandling.MultiStates;
using EDIVE.StateHandling.StateValuePresets;
using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor.Validation;
using UnityEditor;
using UnityEngine;

namespace EDIVE.StateHandling
{
    public static class StateControlEditorUtils
    {
        [MenuItem("CONTEXT/MultiStateObjects/Convert to Presets", false, 10000)]
        public static void ConvertToPresets(MenuCommand command)
        {
            if (command.context is not MultiStateObjects behaviour)
                return;

            var newStatePresets = new List<MultiStateRecord>();
            var allTargets = behaviour._StatePresets.SelectMany(s => s.Targets).ToList();
            foreach (var statePreset in behaviour._StatePresets)
            {
                var newObjectPresets = new List<ObjectStatePresetRecord>();
                foreach (var target in allTargets)
                {
                    var newPreset = new ObjectStatePresetRecord(target);
                    var valuePreset = new GameObjectActivePreset
                    {
                        Value = statePreset.Targets.Contains(target)
                    };
                    newPreset._ValuePresets = new List<AStateValuePreset>{valuePreset};
                    newObjectPresets.Add(newPreset);
                }
                newStatePresets.Add(new MultiStateRecord( statePreset.StateID, newObjectPresets));
            }

            var newBehaviour = behaviour.ChangeScriptType<MultiState>();
            newBehaviour._StatePresets = newStatePresets;
            EditorUtility.SetDirty(newBehaviour);
        }

        [MenuItem("CONTEXT/MultiState/Convert to Objects", false, 10000)]
        public static void ConvertToGameObjects(MenuCommand command)
        {
            if (command.context is not MultiState behaviour)
                return;

            var newPresets = new List<GameObjectRecord>();
            foreach (var statePreset in behaviour._StatePresets)
            {
                var enabledObjects = new List<GameObject>();
                foreach (var objectPreset in statePreset.ObjectPresets)
                {
                    if (objectPreset.Target is GameObject go && objectPreset.ValuePresets.Count == 1 && objectPreset.ValuePresets[0] is GameObjectActivePreset goPreset)
                    {
                        if (goPreset.Value)
                        {
                            enabledObjects.Add(objectPreset.Target as GameObject);
                        }
                    }
                    else
                    {
                        Debug.LogError($"Preset {statePreset.StateID} contains value preset that is not for GameObject!");
                        return;
                    }
                }
                newPresets.Add(new GameObjectRecord(statePreset.StateID, enabledObjects));
            }
            
            var newBehaviour = behaviour.ChangeScriptType<MultiStateObjects>();
            newBehaviour._StatePresets = newPresets;
            EditorUtility.SetDirty(newBehaviour);
        }

        [MenuItem("CONTEXT/ToggleStateObjects/Convert to Presets", false, 10000)]
        public static void ConvertToggleToPresets(MenuCommand command)
        {
            if (command.context is not ToggleStateObjects behaviour)
                return;

            var newStatePresets = new List<ToggleStateRecord>();
            foreach (var onTarget in behaviour._OnTargets)
            {
                var newPreset = new ToggleStateRecord(onTarget)
                {
                    _EnabledPresets = new List<AStateValuePreset>{new GameObjectActivePreset { Value = true }},
                    _DisabledPresets = new List<AStateValuePreset>{new GameObjectActivePreset { Value = false }}
                };
                newStatePresets.Add(newPreset);
            }
            foreach (var offTarget in behaviour._OffTargets)
            {
                var newPreset = new ToggleStateRecord(offTarget)
                {
                    _EnabledPresets = new List<AStateValuePreset>{new GameObjectActivePreset { Value = false }},
                    _DisabledPresets = new List<AStateValuePreset>{new GameObjectActivePreset { Value = true }}
                };
                newStatePresets.Add(newPreset);
            }

            var newBehaviour = behaviour.ChangeScriptType<ToggleState>();
            newBehaviour._ObjectToggleStatePreset = newStatePresets;
            EditorUtility.SetDirty(newBehaviour);
        }

        [MenuItem("CONTEXT/ToggleState/Convert to Objects", false, 10000)]
        public static void ConvertToggleToActiveObjects(MenuCommand command)
        {
            if (command.context is not ToggleState behaviour)
                return;

            var onObjects = new List<GameObject>();
            var offObjects = new List<GameObject>();

            foreach (var statePreset in behaviour._ObjectToggleStatePreset)
            {
                if (statePreset.Target is GameObject go &&
                    statePreset._EnabledPresets.Count == 1 && statePreset._EnabledPresets[0] is GameObjectActivePreset onPreset &&
                    statePreset._DisabledPresets.Count == 1 && statePreset._DisabledPresets[0] is GameObjectActivePreset offPreset)
                {
                    if (onPreset.Value)
                    {
                        onObjects.Add(go);
                    }
                    if (offPreset.Value)
                    {
                        offObjects.Add(go);
                    }
                }
                else
                {
                    Debug.LogError("Preset contains value preset that is not for GameObject!");
                    return;
                }
            }
            var newBehaviour = behaviour.ChangeScriptType<ToggleStateObjects>();
            newBehaviour._OnTargets = onObjects;
            newBehaviour._OffTargets = offObjects;
            EditorUtility.SetDirty(newBehaviour);
        }
        
        public static IEnumerable<ValueDropdownItem<AStateValuePreset>> GetValuePresetDropdown(Type targetType)
        {
            return TypeCacheUtils.GetDerivedClassesOfType<AStateValuePreset>()
                .Where(p => p.IsValidFor(targetType))
                .Select(p => new ValueDropdownItem<AStateValuePreset>(p.Title, p));
        }
        
        public static void ValidateStateValuePresets(Type targetType, List<AStateValuePreset> value, ValidationResult result)
        {
            foreach (var valuePreset in value)
            {
                if (valuePreset == null)
                {
                    result.AddWarning("Contains null preset!")
                        .WithFix(() => value.RemoveAll(v => v == null));
                    continue;
                }

                if (!valuePreset.IsValidFor(targetType))
                {
                    result.AddError($"Preset '{valuePreset}' is not compatible with Target!")
                        .WithFix(() => value.Remove(valuePreset));
                }
            }
        }
    }
}
#endif
