using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.LoadItems;
using EDIVE.AssetTranslation;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.EditorUtils;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.AppLoading
{
    public class LoadGroupDefinition : AUniqueDefinition
    {
        [PropertySpace(6)]
        [Searchable]
        [SerializeField]
        [EnhancedValidate("ValidateLoadItems")]
        private List<ALoadItemDefinition> _LoadItems = new();

        [PropertySpace]
        [PropertyOrder(50)]
        [SerializeField]
        [Searchable]
        [EnhancedValidate("ValidateDependencies")]
        private List<LoadGroupDefinition> _Dependencies = new();

        [PropertySpace]
        [SerializeField]
        private bool _Disabled;

        public UniTaskCompletionSource CompletionSource { get; private set; }

        public List<ALoadItemDefinition> LoadItems => _LoadItems;
        public List<LoadGroupDefinition> Dependencies => _Dependencies;

        public bool IsAvailable => !_Disabled;

        public void Initialize()
        {
            foreach (var loadItem in _LoadItems)
            {
                if (loadItem == null) continue;
                loadItem.Initialize();
            }

            CompletionSource = new UniTaskCompletionSource();
        }

        public void Terminate()
        {
            foreach (var loadItem in _LoadItems)
            {
                if (loadItem == null) continue;
                loadItem.Terminate();
            }

            CompletionSource = null;
        }

        public async UniTask Load()
        {
            var dependencySources = _Dependencies.Where(d => d != null).Select(d => d.CompletionSource.Task);
            await UniTask.WhenAll(dependencySources);

            var itemTasks = GetLoadItems().Select(l => l.Load());
            await UniTask.WhenAll(itemTasks);

            CompletionSource.TrySetResult();
        }

        private IEnumerable<ALoadItemDefinition> GetLoadItems()
        {
            for (var i = 0; i < _LoadItems.Count; i++)
            {
                var loadItem = _LoadItems[i];
                if (loadItem == null)
                {
                    Debug.LogError($"Load item at index {i} in group '{UniqueID}' is null");
                    continue;
                }

                if (loadItem.IsValid)
                {
                    yield return loadItem;
                }
                else
                {
                    Debug.LogError($"[{GetType().Name}] Load item at index {i} '{loadItem.name}' in group '{UniqueID}' is not valid");
                }
            }
        }

        internal void PrepareSorting()
        {
            foreach (var loadItem in _LoadItems)
            {
                if (loadItem == null)
                    continue;
                loadItem.SortingPreparedGroup = this;
            }
        }

#if UNITY_EDITOR
        [PropertySpace]
        [ShowInInspector]
        [Searchable]
        [PropertyOrder(50)]
        [ReadOnlyListElements]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = "TransitiveDependenciesTitleBarGUI")]
        public List<LoadGroupDefinition> TransitiveDependencies { get; private set; }

        [ShowInInspector]
        [Searchable]
        [PropertyOrder(50)]
        [ReadOnlyListElements]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = "DependentByTitleBarGUI")]
        public List<LoadGroupDefinition> DependentBy { get; private set; }
        

        [UsedImplicitly]
        private void TransitiveDependenciesTitleBarGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                ResolveCompleteDependencies();
            }
        }

        [UsedImplicitly]
        private void DependentByTitleBarGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                ResolveDependentBy();
            }
        }

        [OnInspectorInit]
        public void ResolveDependentBy()
        {
            DependentBy ??= new List<LoadGroupDefinition>();
            DependentBy.Clear();
            DependentBy.AddRange(EditorAssetUtils.FindAllAssetsOfType<LoadGroupDefinition>().Where(i => i._Dependencies.Contains(this)));
        }

        [OnInspectorInit]
        public void ResolveCompleteDependencies()
        {
            var transitiveDependencies = new HashSet<LoadGroupDefinition>();

            var queue = new Queue<LoadGroupDefinition>(_Dependencies);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == null || current == this || !transitiveDependencies.Add(current))
                    continue;

                foreach (var dependency in current._Dependencies)
                {
                    if (dependency is null)
                        continue;

                    if (transitiveDependencies.Contains(dependency))
                        continue;

                    queue.Enqueue(dependency);
                }
            }

            TransitiveDependencies ??= new List<LoadGroupDefinition>();
            TransitiveDependencies.Clear();
            TransitiveDependencies.AddRange(transitiveDependencies);
        }

        [UsedImplicitly]
        private void ValidateDependencies(SelfValidationResult result)
        {
            var visited = new HashSet<LoadGroupDefinition>();
            var path = new List<LoadGroupDefinition>();

            var cycle = DetectCycle(this, this, visited, path);
            if (cycle != null && cycle.Count > 0)
            {
                result.AddError($"Cyclic dependency detected: {string.Join("-", cycle.Select(node => node.name))}");
            }

            ResolveCompleteDependencies();
            foreach (var loadItem in TransitiveDependencies.SelectMany(d => d.LoadItems).Distinct())
            {
                if (!loadItem) continue;
                loadItem.ResolveCompleteDependencies();
            }

            var unsatisfiableDependencies = new List<string>();
            foreach (var groupDependency in TransitiveDependencies)
            {
                foreach (var loadItem in groupDependency.LoadItems)
                {
                    if (loadItem == null) continue;
                    var intersection = _LoadItems.Intersect(loadItem.TransitiveDependencies).ToList();
                    if (intersection.Count > 0)
                    {
                        unsatisfiableDependencies.Add($"[{groupDependency.name}:{loadItem.name} - {string.Join(", ", intersection.Select(v => v.name))}]");
                    }
                }
            }

            if (unsatisfiableDependencies.Count > 0)
            {
                result.AddError($"Unsatisfiable dependencies: {string.Join(", ", unsatisfiableDependencies)}");
            }
        }

        [UsedImplicitly]
        private void ValidateLoadItems(SelfValidationResult result)
        {
            var duplicateItems = _LoadItems.GroupBy(i => i)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicateItems.Count > 0)
            {
                result.AddError($"There are duplicate items: {string.Join(", ", duplicateItems.Select(i => i.Key.name))}");
            }
        }

        private List<LoadGroupDefinition> DetectCycle(LoadGroupDefinition node, LoadGroupDefinition root, HashSet<LoadGroupDefinition> visited, List<LoadGroupDefinition> path)
        {
            if (node == null)
                return null;

            visited.Add(node);
            path.Add(node);

            foreach (var dependency in node._Dependencies)
            {
                if (!visited.Contains(dependency))
                {
                    var cycle = DetectCycle(dependency, root, visited, path);
                    if (cycle != null && cycle.Count > 0)
                        return cycle;
                }
                else if (dependency == root)
                {
                    return path.ToList();
                }
            }

            path.Remove(node);
            return null;
        }

        protected override string FormatFileNameForID(string filename) => filename.Replace("Definition", "");
#endif
    }
}
