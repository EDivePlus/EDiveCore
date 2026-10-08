// Author: František Holubec
// Created: 08.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.View.ViewTree.Editor
{
    // Scene or prefab stage shown in the View Tree window
    public abstract class AViewTreeSource : IDisposable
    {
        private List<ViewTreeItem> _roots;
        public List<ViewTreeItem> Roots => _roots ??= BuildRoots();

        public string Name { get; protected set; }
        public Texture Icon { get; protected set; }

        public bool HasProblems => EnumerateItems().Any(i => i.SelfInvalid);

        // False once the scene is unloaded or the prefab stage closed
        public abstract bool IsValid { get; }

        protected abstract IEnumerable<GameObject> GetRootObjects();

        public void Rebuild()
        {
            Dispose();
            _roots = BuildRoots();
        }

        public IEnumerable<ViewTreeItem> EnumerateItems() => Roots.SelectMany(r => r.EnumerateRecursively());

        // False when rows of the changed objects no longer match their name, parent or missing adapters
        public bool IsCurrent(ICollection<GameObject> changed)
        {
            var nodes = new HashSet<AViewNode>();
            foreach (var item in EnumerateItems())
            {
                if (item.Target == null)
                    return false;
                if (item.Node != null)
                    nodes.Add(item.Node);
            }

            var missingRows = new HashSet<Component>();
            foreach (var item in EnumerateItems())
            {
                if (!changed.Contains(item.GameObject))
                    continue;
                if (item.Name != ViewTreeItem.GetName(item.GameObject))
                    return false;

                if (item.Node != null)
                {
                    var parent = HasCycle(item.Node) ? null : GetParent(item.Node);
                    var expected = parent != null && nodes.Contains(parent) ? parent : null;
                    if (item.Parent?.Node != expected)
                        return false;
                    continue;
                }

                if (!ViewTreeEditorUtils.IsMissingAdapter(item.MissingAdapterComponent, out _))
                    return false;
                missingRows.Add(item.MissingAdapterComponent);
            }

            if (Application.isPlaying)
                return true;

            foreach (var gameObject in changed)
            {
                if (gameObject == null)
                    continue;

                foreach (var component in gameObject.GetComponents<Component>())
                {
                    if (ViewTreeEditorUtils.IsMissingAdapter(component, out _) && !missingRows.Contains(component))
                        return false;
                }
            }
            return true;
        }

        public void FixAll()
        {
            foreach (var item in EnumerateItems().ToList())
                item.Fix();
        }

        // Logical tree: runtime parents in play mode, designated parents in edit mode
        private List<ViewTreeItem> BuildRoots()
        {
            var nodes = GetRootObjects().SelectMany(root => root.GetComponentsInChildren<AViewNode>(true)).ToList();
            var items = nodes.ToDictionary(node => node, node => new ViewTreeItem(node));
            var roots = new List<ViewTreeItem>();

            foreach (var node in nodes)
            {
                var parent = GetParent(node);
                if (parent != null && items.TryGetValue(parent, out var parentItem) && !HasCycle(node))
                    parentItem.AddChild(items[node]);
                else
                    roots.Add(items[node]);
            }

            // Validation is edit mode only
            if (!Application.isPlaying)
                AddMissingAdapterItems(nodes, items);
            return roots;
        }

        // Under the nearest group, or the node on the same object
        private static void AddMissingAdapterItems(List<AViewNode> nodes, Dictionary<AViewNode, ViewTreeItem> items)
        {
            var missing = new HashSet<Component>();
            foreach (var node in nodes)
            {
                var components = node is ViewGroup group
                    ? ViewTreeEditorUtils.GetComponentsMissingAdapter(group)
                    : node.GetComponents<Component>().Where(c => ViewTreeEditorUtils.IsMissingAdapter(c, out _));
                missing.UnionWith(components);
            }

            foreach (var component in missing)
            {
                AViewNode owner = component.GetComponentInParent<ViewGroup>(true);
                if (owner == null)
                    component.TryGetComponent(out owner);
                if (owner != null && items.TryGetValue(owner, out var ownerItem))
                    ownerItem.AddChild(new ViewTreeItem(component));
            }
        }

        private static ViewGroup GetParent(AViewNode node) => Application.isPlaying ? node.Parent : node.GetDesignatedParent();

        private static bool HasCycle(AViewNode node)
        {
            var visited = new HashSet<AViewNode> { node };
            for (var parent = GetParent(node); parent != null; parent = GetParent(parent))
            {
                if (!visited.Add(parent))
                    return true;
            }
            return false;
        }

        public override string ToString() => Name;

        public void Dispose()
        {
            if (_roots == null)
                return;

            foreach (var root in _roots)
                root.Dispose();
            _roots = null;
        }
    }

    public class SceneViewTreeSource : AViewTreeSource
    {
        public Scene Scene { get; }

        public SceneViewTreeSource(Scene scene)
        {
            Scene = scene;
            Name = scene.name;
            Icon = EditorGUIUtility.IconContent("SceneAsset Icon").image;
        }

        public override bool IsValid => Scene.IsValid() && Scene.isLoaded;

        protected override IEnumerable<GameObject> GetRootObjects() => IsValid ? Scene.GetRootGameObjects() : Array.Empty<GameObject>();

        public override bool Equals(object obj) => obj is SceneViewTreeSource other && other.Scene == Scene;
        public override int GetHashCode() => Scene.GetHashCode();
    }

    public class PrefabStageViewTreeSource : AViewTreeSource
    {
        public PrefabStage Stage { get; }

        public PrefabStageViewTreeSource(PrefabStage stage)
        {
            Stage = stage;
            Name = stage.prefabContentsRoot.name;
            Icon = EditorGUIUtility.IconContent("Prefab Icon").image;
        }

        public override bool IsValid => Stage != null && Stage.prefabContentsRoot != null;

        protected override IEnumerable<GameObject> GetRootObjects()
        {
            if (IsValid)
                yield return Stage.prefabContentsRoot;
        }

        public override bool Equals(object obj) => obj is PrefabStageViewTreeSource other && other.Stage == Stage;
        public override int GetHashCode() => Stage != null ? Stage.GetHashCode() : 0;
    }
}
