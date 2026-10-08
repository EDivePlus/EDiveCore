// Author: František Holubec
// Created: 08.10.2026

using System;
using System.Collections.Generic;
using EDIVE.OdinExtensions;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EDIVE.View.ViewTree.Editor
{
    public static class ViewTreeEditorUtils
    {
        public static readonly AttachMode[] ATTACH_MODES = { AttachMode.Auto, AttachMode.Explicit, AttachMode.Manual };

        private static readonly Color AUTO_COLOR = new Color32(0x4D, 0xD0, 0xE1, 0xFF);
        private static readonly Color EXPLICIT_COLOR = new Color32(0xFF, 0xCA, 0x6B, 0xFF);
        private static readonly Color SCRIPT_COLOR = new Color32(0xB3, 0x9D, 0xDB, 0xFF);

        private const string TARGET_FIELD = "_Target";
        private const string EXPLICIT_PARENT_FIELD = "_ExplicitParent";
        private const string ATTACH_MODE_FIELD = "_AttachMode";

        private static Dictionary<Type, ViewAdapterAttribute> _adapterMappings;
        private static Dictionary<Type, Type> _adapterTypes;

        public static EditorIcon GetIcon(this AttachMode mode) => mode switch
        {
            AttachMode.Auto => FontAwesomeEditorIcons.BoltSolid,
            AttachMode.Explicit => FontAwesomeEditorIcons.LinkSimpleSolid,
            AttachMode.Manual => FontAwesomeEditorIcons.CodeSolid,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        public static Color GetColor(this AttachMode mode) => mode switch
        {
            AttachMode.Auto => AUTO_COLOR,
            AttachMode.Explicit => EXPLICIT_COLOR,
            AttachMode.Manual => SCRIPT_COLOR,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        // Native component type -> adapter that should handle it
        public static IReadOnlyDictionary<Type, Type> AdapterTypes
        {
            get
            {
                BuildAdapterMappings();
                return _adapterTypes;
            }
        }

        private static void BuildAdapterMappings()
        {
            if (_adapterTypes != null)
                return;

            _adapterTypes = new Dictionary<Type, Type>();
            _adapterMappings = new Dictionary<Type, ViewAdapterAttribute>();

            var declared = new Dictionary<Type, Type>();
            foreach (var adapterType in TypeCache.GetTypesWithAttribute<ViewAdapterAttribute>())
            {
                if (adapterType.IsAbstract || adapterType.IsGenericType)
                    continue;

                var componentType = adapterType.GetAttribute<ViewAdapterAttribute>().ComponentType;
                if (componentType == null)
                    continue;

                if (declared.TryGetValue(componentType, out var other))
                    Debug.LogWarning($"[View Tree] {componentType.Name} is mapped by both {other.Name} and {adapterType.Name}.");
                declared[componentType] = adapterType;
            }

            // Derived types take the closest declared base, so a more specific adapter wins
            foreach (var componentType in declared.Keys)
            {
                Map(componentType, declared);
                foreach (var derivedType in TypeCache.GetTypesDerivedFrom(componentType))
                    Map(derivedType, declared);
            }
        }

        private static void Map(Type componentType, Dictionary<Type, Type> declared)
        {
            for (var type = componentType; type != null; type = type.BaseType)
            {
                if (!declared.TryGetValue(type, out var adapterType))
                    continue;

                _adapterTypes[componentType] = adapterType;
                _adapterMappings[componentType] = adapterType.GetAttribute<ViewAdapterAttribute>();
                return;
            }
        }

        public static bool IsAdapterRequired(Type componentType)
        {
            BuildAdapterMappings();
            return _adapterMappings.TryGetValue(componentType, out var attribute) && attribute.Required;
        }

        // Node on the same object or a group above
        public static bool IsInViewTree(Component component) => component.TryGetComponent<AViewNode>(out _) || component.GetComponentInParent<ViewGroup>(true) != null;

        public static bool IsMissingAdapter(Component component, out Type adapterType)
        {
            adapterType = null;
            return component != null
                && AdapterTypes.TryGetValue(component.GetType(), out adapterType)
                && !ViewAdapterUtility.IsHandled(component)
                && IsInViewTree(component);
        }

        // Mapped components in the group scope, stops at nested groups
        public static IEnumerable<Component> GetComponentsMissingAdapter(ViewGroup group)
        {
            var stack = new Stack<Transform>();
            stack.Push(group.transform);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current != group.transform && current.TryGetComponent<ViewGroup>(out _))
                    continue;

                foreach (var component in current.GetComponents<Component>())
                {
                    if (IsMissingAdapter(component, out _))
                        yield return component;
                }

                for (var i = current.childCount - 1; i >= 0; i--)
                    stack.Push(current.GetChild(i));
            }
        }

        public static void ValidateMissingAdapter(Component component, ValidationResult result, InspectorProperty property = null)
        {
            if (!IsMissingAdapter(component, out var adapterType))
                return;

            var message = $"[View Tree] {component.GetType().Name} is not handled by the view tree.";
            var fixName = $"Add {adapterType.Name}";
            void AddAdapter()
            {
                var adapter = Undo.AddComponent(component.gameObject, adapterType);
                SetTarget(adapter, component);
                MarkDirty(component, property);
            }

            if (IsAdapterRequired(component.GetType()))
                result.AddError(message).WithFix(fixName, AddAdapter);
            else
                result.AddWarning(message).WithFix(fixName, AddAdapter);
        }

        public static void Validate(AViewNode node, ValidationResult result, InspectorProperty property = null)
        {
            ValidateAttachment(node, result);
            ValidateTarget(node, result, property);
            ValidateActiveWhile(node, result);
        }

        private static void ValidateActiveWhile(AViewNode node, ValidationResult result)
        {
            if (node is AToggleViewAdapter { ActiveWhile: ViewState.Hidden })
                result.AddWarning("[View Tree] Active While Hidden keeps it always on, use Visible or Focused.");
        }

        private static void ValidateAttachment(AViewNode node, ValidationResult result)
        {
            // Prefab roots get their explicit parent on the instance
            if (node.AttachMode == AttachMode.Explicit && node.ExplicitParent == null && !IsPrefabContext(node))
            {
                const string MESSAGE = "[View Tree] Explicit node has no parent assigned.";
                var hierarchyParent = node.FindHierarchyParent();
                if (hierarchyParent != null)
                    result.AddError(MESSAGE).WithFix($"Use {hierarchyParent.name}", () => SetSerializedReference(node, EXPLICIT_PARENT_FIELD, hierarchyParent));
                else
                    result.AddError(MESSAGE).WithFix("Switch to Auto", () => SetSerializedEnum(node, ATTACH_MODE_FIELD, (int) AttachMode.Auto));
            }

            if (HasParentCycle(node))
                result.AddError("[View Tree] Explicit parents form a cycle, these nodes never attach.");
        }

        private static bool HasParentCycle(AViewNode node)
        {
            var visited = new HashSet<AViewNode> { node };
            for (var parent = node.GetDesignatedParent(); parent != null; parent = parent.GetDesignatedParent())
            {
                if (!visited.Add(parent))
                    return parent == node;
            }
            return false;
        }

        private static void ValidateTarget(AViewNode node, ValidationResult result, InspectorProperty property)
        {
            if (node is not IComponentViewAdapter adapter)
                return;

            var target = adapter.Target;
            var typeName = adapter.TargetType.Name;
            if (target == null)
            {
                var unhandled = ViewAdapterUtility.FindUnhandledTarget(node.gameObject, adapter.TargetType);
                if (unhandled == null)
                {
                    result.AddError($"[View Tree] No free {typeName} on this object, nothing to adapt.")
                        .WithFix("Remove adapter", () => Undo.DestroyObjectImmediate(node));
                    return;
                }

                result.AddError($"[View Tree] No {typeName} assigned.")
                    .WithFix($"Assign {unhandled.GetType().Name}", () => AssignTarget(node, unhandled, property));
                return;
            }

            if (target.gameObject != node.gameObject)
            {
                const string MESSAGE = "[View Tree] Target is on another object, it must be on this one.";
                var unhandled = ViewAdapterUtility.FindUnhandledTarget(node.gameObject, adapter.TargetType);
                if (unhandled != null)
                    result.AddError(MESSAGE).WithFix($"Assign {unhandled.GetType().Name} here", () => AssignTarget(node, unhandled, property));
                else
                    result.AddError(MESSAGE);
                return;
            }

            foreach (var other in node.GetComponents<IComponentViewAdapter>())
            {
                if (!ReferenceEquals(other, adapter) && other.Target == target)
                {
                    result.AddWarning($"[View Tree] Another adapter handles the same {typeName}.")
                        .WithFix("Remove this adapter", () => Undo.DestroyObjectImmediate(node));
                    return;
                }
            }
        }

        private static void AssignTarget(AViewNode node, Component target, InspectorProperty property)
        {
            SetTarget(node, target);
            MarkDirty(node, property);
        }

        public static void SetTarget(Component adapter, Component target) => SetSerializedReference(adapter, TARGET_FIELD, target);

        // Through SerializedObject for undo and prefab overrides
        private static void SetSerializedReference(Component component, string field, Object value)
        {
            var serializedObject = new SerializedObject(component);
            var property = FindSerializedProperty(serializedObject, field);
            if (property == null)
                return;

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private static void SetSerializedEnum(Component component, string field, int value)
        {
            var serializedObject = new SerializedObject(component);
            var property = FindSerializedProperty(serializedObject, field);
            if (property == null)
                return;

            property.enumValueIndex = value;
            serializedObject.ApplyModifiedProperties();
        }

        // Custom nodes may name their fields differently
        private static SerializedProperty FindSerializedProperty(SerializedObject serializedObject, string field)
        {
            var property = serializedObject.FindProperty(field);
            if (property == null)
                Debug.LogWarning($"[View Tree] {serializedObject.targetObject.GetType().Name} has no {field} field, set it by hand.", serializedObject.targetObject);
            return property;
        }

        public static bool IsPrefabContext(Component component)
        {
            return PrefabUtility.IsPartOfPrefabAsset(component) || PrefabStageUtility.GetPrefabStage(component.gameObject) != null;
        }

        public static void MarkDirty(Component component, InspectorProperty property = null)
        {
            EditorUtility.SetDirty(component);
            if (component.gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            property?.ForceMarkDirty();
        }

        public static (Texture icon, string tooltip) GetObjectIcon(GameObject obj)
        {
            if (obj == null || !PrefabUtility.IsPartOfPrefabInstance(obj))
                return (EditorGUIUtility.IconContent("GameObject Icon").image, "Game Object");

            return PrefabUtility.IsAnyPrefabInstanceRoot(obj)
                ? (EditorGUIUtility.IconContent("Prefab Icon").image, "Prefab Root")
                : (EditorGUIUtility.IconContent("Prefab On Icon").image, "Part of Prefab");
        }
    }
}
