// Author: František Holubec
// Created: 09.10.2026

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace EDIVE.EditorUtils.EditorPriority
{
    // Moves [EditorPriority] editors to the front of Unity's internal editor lists
    public static class EditorPriorityUtility
    {
        private const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static IDictionary _cache;
        private static Type _storageType;
        private static Type _entryType;
        private static FieldInfo _editorsField;
        private static FieldInfo _multiEditorsField;
        private static FieldInfo _inspectorTypeField;
        private static FieldInfo _pipelineTypesField;
        private static PropertyInfo _instanceProperty;
        private static FieldInfo _cacheField;
        private static FieldInfo _cacheDictionaryField;
        private static bool _isResolved;

        private static Type[] _editorTypes;
        private static Dictionary<Type, Type> _topEditors;
        private static bool _isApplyQueued;


        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // Lowest first, each one is inserted in front, so the highest ends up first
            _editorTypes = TypeCache.GetTypesWithAttribute<EditorPriorityAttribute>()
                .Where(t => !t.IsAbstract && typeof(Editor).IsAssignableFrom(t) && t.GetCustomAttribute<CustomEditor>() != null)
                .OrderBy(t => t.GetCustomAttribute<EditorPriorityAttribute>().Priority)
                .ToArray();
            if (_editorTypes.Length == 0)
                return;

            // Highest priority editor per inspected type, the expected first entry
            _topEditors = new Dictionary<Type, Type>();
            foreach (var editorType in _editorTypes)
                _topEditors[CustomEditorUtility.GetInspectedType(editorType.GetCustomAttribute<CustomEditor>())] = editorType;

            // Same priority on one type leaves the winner to Unity again
            var ties = _editorTypes
                .GroupBy(t => (Inspected: CustomEditorUtility.GetInspectedType(t.GetCustomAttribute<CustomEditor>()), t.GetCustomAttribute<EditorPriorityAttribute>().Priority))
                .Where(group => group.Count() > 1);
            foreach (var tie in ties)
                Debug.LogWarning($"[EditorPriority] {string.Join(", ", tie.Select(t => t.Name))} share priority {tie.Key.Priority} for {tie.Key.Inspected.Name}, give them different priorities.");

            // Unity rebuilds its editor cache on its own, so check whenever inspectors rebuild
            var rebuiltEvent = typeof(ActiveEditorTracker).GetEvent("editorTrackerRebuilt", FLAGS);
            rebuiltEvent?.GetAddMethod(true)?.Invoke(null, new object[] { (Action) OnEditorTrackerRebuilt });

            QueueApply();
        }

        private static void OnEditorTrackerRebuilt()
        {
            if (!IsApplied())
                QueueApply();
        }

        private static void QueueApply()
        {
            if (_isApplyQueued)
                return;

            // Not delayCall, other code may overwrite it
            _isApplyQueued = true;
            EditorApplication.update += ApplyQueued;
        }

        private static void ApplyQueued()
        {
            EditorApplication.update -= ApplyQueued;
            _isApplyQueued = false;
            Apply();
        }

        // Top editor is first, with pipeline types when pipeline specific editors compete
        private static bool IsApplied()
        {
            if (!TryResolveInternals())
                return true;

            foreach (var (inspectedType, topEditor) in _topEditors)
            {
                if (!_cache.Contains(inspectedType))
                    return false;

                var entries = (IList) _editorsField.GetValue(_cache[inspectedType]);
                if (entries == null || entries.Count == 0 || (Type) _inspectorTypeField.GetValue(entries[0]) != topEditor)
                    return false;

                var competesOnPipeline = entries.Cast<object>().Any(e => (Type) _inspectorTypeField.GetValue(e) != topEditor && _pipelineTypesField.GetValue(e) != null);
                if (competesOnPipeline && _pipelineTypesField.GetValue(entries[0]) == null)
                    return false;
            }
            return true;
        }

        public static void Apply()
        {
            if (!TryResolveInternals())
            {
                Debug.LogWarning("[EditorPriority] Unity editor cache internals changed, [EditorPriority] has no effect.");
                return;
            }

            foreach (var editorType in _editorTypes)
            {
                var attribute = editorType.GetCustomAttribute<CustomEditor>();
                var inspectedType = CustomEditorUtility.GetInspectedType(attribute);
                var forChildren = CustomEditorUtility.IsForChildClasses(attribute);
                var multiEdit = editorType.GetCustomAttribute<CanEditMultipleObjects>() != null;

                Prioritize(inspectedType, editorType, forChildren, multiEdit);
                if (!forChildren)
                    continue;

                foreach (var derivedType in TypeCache.GetTypesDerivedFrom(inspectedType))
                    Prioritize(derivedType, editorType, true, multiEdit);
            }

            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        // Reflection resolved once, the cache dictionary is fetched each time since Unity may replace it
        private static bool TryResolveInternals()
        {
            if (!_isResolved)
            {
                var assembly = typeof(Editor).Assembly;
                var attributesType = assembly.GetType("UnityEditor.CustomEditorAttributes");
                _entryType = assembly.GetType("UnityEditor.CustomEditorAttributes+MonoEditorType");
                _instanceProperty = attributesType?.GetProperty("instance", FLAGS);
                _cacheField = attributesType?.GetField("m_Cache", FLAGS);
                _cacheDictionaryField = _cacheField?.FieldType.GetField("m_CustomEditorCache", FLAGS);
                _storageType = _cacheDictionaryField?.FieldType.GetGenericArguments()[1];
                _editorsField = _storageType?.GetField("customEditors", FLAGS);
                _multiEditorsField = _storageType?.GetField("customEditorsMultiEdition", FLAGS);
                _inspectorTypeField = _entryType?.GetField("inspectorType", FLAGS);
                _pipelineTypesField = _entryType?.GetField("supportedRenderPipelineTypes", FLAGS);
                _isResolved = true;
            }

            if (_instanceProperty == null || _cacheDictionaryField == null || _editorsField == null || _multiEditorsField == null || _inspectorTypeField == null || _pipelineTypesField == null)
                return false;

            var cache = _cacheField.GetValue(_instanceProperty.GetValue(null));
            _cache = cache != null ? _cacheDictionaryField.GetValue(cache) as IDictionary : null;
            return _cache != null;
        }

        private static void Prioritize(Type inspectedType, Type editorType, bool forChildren, bool multiEdit)
        {
            if (!_cache.Contains(inspectedType))
                _cache[inspectedType] = Activator.CreateInstance(_storageType, true);

            var storage = _cache[inspectedType];
            Prioritize((IList) _editorsField.GetValue(storage), editorType, forChildren);
            if (multiEdit)
                Prioritize((IList) _multiEditorsField.GetValue(storage), editorType, forChildren);
        }

        private static void Prioritize(IList entries, Type editorType, bool forChildren)
        {
            if (entries == null)
                return;

            for (var i = entries.Count - 1; i >= 0; i--)
            {
                if ((Type) _inspectorTypeField.GetValue(entries[i]) == editorType)
                    entries.RemoveAt(i);
            }

            // Pipeline specific editors are looked up first, so compete with them on their pipelines too
            var pipelineTypes = entries.Cast<object>()
                .Select(e => (Type[]) _pipelineTypesField.GetValue(e))
                .Where(types => types != null)
                .SelectMany(types => types)
                .Distinct()
                .ToArray();

            entries.Insert(0, CreateEntry(editorType, null, forChildren));
            if (pipelineTypes.Length > 0)
                entries.Insert(0, CreateEntry(editorType, pipelineTypes, forChildren));
        }

        private static object CreateEntry(Type editorType, Type[] pipelineTypes, bool forChildren)
        {
            return Activator.CreateInstance(_entryType, FLAGS, null, new object[] { editorType, pipelineTypes, forChildren, false }, null);
        }
    }
}
