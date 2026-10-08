// Author: František Holubec
// Created: 08.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace EDIVE.View.ViewTree.Editor
{
    // One row in the View Tree window, a node or a component missing its adapter
    public class ViewTreeItem : IDisposable
    {
        public AViewNode Node { get; }
        public Component MissingAdapterComponent { get; }
        public GameObject GameObject { get; }

        // Node or the component missing its adapter
        public UnityEngine.Object Target => Node != null ? Node : MissingAdapterComponent;
        public string Name { get; }
        public string TypeName { get; }
        public string Path { get; private set; }

        public ViewTreeItem Parent { get; private set; }
        public List<ViewTreeItem> Children { get; } = new();

        public bool SelfInvalid => _problems.Count > 0;
        public bool ChildInvalid { get; private set; }

        [ShowIf(nameof(SelfInvalid))]
        [ShowInInspector]
        [EnhancedBoxGroup(ITEM_GROUP, LabelText = "$Path")]
        [PropertyOrder(10)]
        [PropertySpace(4)]
        [LabelText("Problems")]
        [ListDrawerSettings(IsReadOnly = true, ShowFoldout = false)]
        private List<ValidationResult> _problems = new();

        private PropertyTree _propertyTree;
        private InspectorProperty _viewGroupProperty;

        private const string VIEW_GROUP_NAME = "#View";

        // One box per selected row, titled with its tree path
        private const string ITEM_GROUP = "Item";

        public ViewTreeItem(AViewNode node)
        {
            Node = node;
            GameObject = node.gameObject;
            Name = GetName(GameObject);
            TypeName = node.GetType().GetNiceName();
        }

        public ViewTreeItem(Component missingAdapterComponent)
        {
            MissingAdapterComponent = missingAdapterComponent;
            GameObject = missingAdapterComponent.gameObject;
            Name = GetName(GameObject);
            TypeName = $"{missingAdapterComponent.GetType().GetNiceName()} without adapter";
        }

        // Slash would split the menu path
        public static string GetName(GameObject gameObject) => gameObject.name.Replace('/', '∕');

        public void AddChild(ViewTreeItem child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        [OnInspectorGUI]
        [PropertyOrder(-10)]
        [EnhancedBoxGroup(ITEM_GROUP, LabelText = "$Path")]
        private void DrawHeader()
        {
            if (GameObject == null)
            {
                SirenixEditorGUI.WarningMessageBox("Invalid node, rebuild the tree.");
                return;
            }

            EditorGUILayout.TextField("Object Path", GameObject.transform.GetPath());
            if (Node is not ViewGroup)
                return;

            SirenixEditorGUI.BeginHorizontalPropertyLayout(GUIHelper.TempContent("Set All Children"));
            foreach (var mode in ViewTreeEditorUtils.ATTACH_MODES)
            {
                GUIHelper.PushColor(mode.GetColor());
                if (GUILayout.Button(GUIHelper.TempContent($" {mode}", mode.GetIcon().Highlighted), GUILayout.Height(18)))
                    SetChildrenAttachMode(mode);
                GUIHelper.PopColor();
            }
            SirenixEditorGUI.EndHorizontalPropertyLayout();
        }

        [OnInspectorGUI]
        [PropertyOrder(100)]
        [EnhancedBoxGroup(ITEM_GROUP, LabelText = "$Path")]
        [PropertySpace(8)]
        private void DrawNodeProperties()
        {
            if (Node == null)
                return;

            if (_propertyTree == null)
            {
                _propertyTree = PropertyTree.Create(Node);
                _propertyTree.OnPropertyValueChanged += (_, _) => ValidateUpwards();
                _viewGroupProperty = _propertyTree.RootProperty.Children.FirstOrDefault(p => p.Info.PropertyType == PropertyType.Group && p.Name == VIEW_GROUP_NAME);
            }

            _propertyTree.BeginDraw(true);
            // Fields only, the View box header adds nothing here
            if (_viewGroupProperty != null)
            {
                foreach (var child in _viewGroupProperty.Children)
                    child.Draw();
            }
            _propertyTree.EndDraw();
        }

        [OnInspectorDispose]
        private void DisposePropertyTree()
        {
            _propertyTree?.Dispose();
            _propertyTree = null;
            _viewGroupProperty = null;
        }

        public void SetChildrenAttachMode(AttachMode mode)
        {
            foreach (var child in Children)
                SetAttachMode(child.Node, mode);
            ValidateRecursively();
        }

        public static void SetAttachMode(AViewNode node, AttachMode mode)
        {
            if (node == null || node.AttachMode == mode)
                return;

            Undo.RecordObject(node, "Set Attach Mode");

            // Keeps the current parent when switching to Explicit, set first so runtime never sees an empty one
            if (mode == AttachMode.Explicit && node.ExplicitParent == null)
                node.ExplicitParent = Application.isPlaying ? node.Parent : node.FindHierarchyParent();
            node.AttachMode = mode;

            ViewTreeEditorUtils.MarkDirty(node);
        }

        public void SetRootPath(string path) => Path = path;

        public void RefreshPaths()
        {
            var usedNames = new HashSet<string>();
            foreach (var child in Children)
            {
                var pathName = child.Name;
                for (var i = 1; !usedNames.Add(pathName); i++)
                    pathName = $"{child.Name}#{i}";

                child.Path = $"{Path}/{pathName}";
            }

            foreach (var child in Children)
                child.RefreshPaths();
        }

        public IEnumerable<ViewTreeItem> EnumerateRecursively()
        {
            yield return this;
            foreach (var item in Children.SelectMany(c => c.EnumerateRecursively()))
                yield return item;
        }

        public void ValidateRecursively()
        {
            foreach (var child in Children)
                child.ValidateRecursively();
            ValidateSelf();
        }

        public void ValidateUpwards()
        {
            ValidateSelf();
            Parent?.ValidateUpwards();
        }

        private void ValidateSelf()
        {
            _problems.Clear();

            // Validation is edit mode only, like the validators
            if (Application.isPlaying)
            {
                ChildInvalid = false;
                return;
            }

            var result = new ValidationResult();
            if (Node != null)
                ViewTreeEditorUtils.Validate(Node, result);
            else if (MissingAdapterComponent != null)
                ViewTreeEditorUtils.ValidateMissingAdapter(MissingAdapterComponent, result);

            result.Explode(ref _problems);
            ChildInvalid = Children.Any(c => c.SelfInvalid || c.ChildInvalid);
        }

        public void Fix()
        {
            InvokeFixes();
            ValidateUpwards();
        }

        public void FixRecursively()
        {
            foreach (var item in EnumerateRecursively())
                item.InvokeFixes();
            ValidateRecursively();
            Parent?.ValidateUpwards();
        }

        private void InvokeFixes()
        {
            foreach (var problem in _problems.ToList())
                problem[0].Fix?.Action?.DynamicInvoke();
        }

        public void Dispose()
        {
            DisposePropertyTree();
            foreach (var child in Children)
                child.Dispose();
        }
    }
}
