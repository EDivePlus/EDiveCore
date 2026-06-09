using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using EDIVE.AddressableAssets;
using EDIVE.AppLoading.Dependencies;
using EDIVE.AppLoading.Utils;
using EDIVE.AssetTranslation;
using EDIVE.Conditions;
using EDIVE.DataStructures.TypeStructures;
using EDIVE.External.Signals;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.AppLoading.LoadItems
{
    public class LoadItemDefinition : AUniqueDefinition, IComparable<LoadItemDefinition>, ISerializationCallbackReceiver
    {
        [SerializeReference]
        [EnhancedBoxGroup("Source", false, Color = "@ColorTools.Lime")]
        [EnhancedTypeSelector(true, 1, OnTypeChanged = "OnSourceChanged")]
        [EnhancedValidate("ValidateSource")]
        [OnValueChanged("RefreshResolvedData", true)]
        protected ALoadItemSource _Source;
        
        [SerializeReference]
        [EnhancedBoxGroup("Condition", false, Color = "@ColorTools.Cyan", SpaceBefore = 4)]
        protected ICondition _Condition;

        [PropertySpace]
        [SerializeField]
        private float _LoadWeight = 1f;

        [SerializeField]
        private bool _FakeLoadingTime;

        [SerializeField]
        [SuffixLabel("s", true)]
        [ShowIf(nameof(_FakeLoadingTime))]
        private float _FakeLoadingTimeDuration = 2;
        
        [EnhancedBoxGroup("Dependencies", Color = "@ColorTools.Orange", Order = 20, SpaceBefore = 8)]
        [SerializeField]
        [InlineProperty]
        [EnhancedValidate("ValidateResolvedData")]
        [ListDrawerSettings(IsReadOnly = true)]
        private List<UType> _RepresentedTypes = new();

        [EnhancedBoxGroup("Dependencies")]
        [SerializeField]
        [ListDrawerSettings(IsReadOnly = true)]
        [EnhancedValidate("ValidateDependencies")]
        private List<TypedDependency> _ResolvedDependencies = new();

        [EnhancedBoxGroup("Dependencies")]
        [SerializeField]
        [EnhancedTableList]
        private List<LoadItemDependency> _ManualDependencies = new();
        
        [ReadOnly]
        [ShowInInspector]
        public bool IsValid => _Source != null && _Source.IsValid;
        
        [ShowInInspector]
        [ReadOnly]
        public LoadItemState CurrentState { get; private set; } = LoadItemState.Undefined;
        
        public float LoadWeight => _LoadWeight;
        public IReadOnlyList<UType> RepresentedTypes => _RepresentedTypes;
        public IReadOnlyList<TypedDependency> ResolvedDependencies => _ResolvedDependencies;
        public IReadOnlyList<LoadItemDependency> ManualDependencies => _ManualDependencies;
        
        public UniTaskCompletionSource CompletionSource { get; private set; }
        
        public bool IsLoaded => CurrentState == LoadItemState.Completed;
        public float LoadTime { get; private set; }
        
        public Signal<LoadItemDefinition> LoadStartedSignal { get; } = new();
        public Signal<LoadItemDefinition> LoadCompletedSignal { get; } = new();

        private Tweener _fakeLoadingTimeTweener;
        private float _currentLoadProgress;
        private DependencyResolutionContext _runtimeContext;

        internal LoadGroupDefinition SortingPreparedGroup;

        private const float MAX_FAKE_PROGRESS = 0.9f;

        public void Initialize(DependencyResolutionContext context)
        {
            CompletionSource = new UniTaskCompletionSource();
            CurrentState = LoadItemState.Pending;
            _currentLoadProgress = 0f;
            _runtimeContext = context;
        }

        public void Terminate()
        {
            CompletionSource = null;
            _runtimeContext = null;
        }
        
        public bool CheckAvailability()
        {
            return IsValid && (_Condition == null || _Condition.Evaluate());
        }

        private IEnumerable<LoadItemDefinition> ResolveDirectDependencies(DependencyResolutionContext context)
        {
            if (context == null)
                yield break;

            var seen = new HashSet<LoadItemDefinition>();
            foreach (var resolvedDependency in _ResolvedDependencies)
            {
                if (resolvedDependency == null) continue;
                foreach (var item in resolvedDependency.Resolve(context))
                {
                    if (item == null || item == this) continue;
                    if (!item.CheckAvailability()) continue;
                    if (seen.Add(item)) yield return item;
                }
            }

            foreach (var manualDependency in _ManualDependencies)
            {
                if (manualDependency == null) continue;
                var dep = manualDependency.Definition;
                if (dep == null || dep == this) continue;
                if (!dep.CheckAvailability()) continue;
                if (seen.Add(dep)) yield return dep;
            }
        }

        public int CompareTo(LoadItemDefinition other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return 0;
        }

        public async UniTask Load()
        {
            if (_runtimeContext == null)
                return;

            var resolvedDependencies = ResolveDirectDependencies(_runtimeContext).ToList();

            var dependencySources = resolvedDependencies
                .Where(i => i.IsValid && i.CompletionSource != null)
                .Select(i => i.CompletionSource.Task);

            var invalidDependencies = resolvedDependencies
                .Where(i => !i.IsValid || i.CompletionSource == null)
                .Select(i => i.name)
                .ToList();
            if (invalidDependencies.Any())
            {
                Debug.LogError($"Invalid resolved dependencies for '{name}': {string.Join(", ", invalidDependencies)}");
            }

            foreach (var record in _ResolvedDependencies)
            {
                if (record != null && !record.Resolve(_runtimeContext).Any())
                    Debug.LogError($"[{name}] Required typed dependency '{record.Type?.Name ?? "null"}' resolved to no items in the active setup.");
            }
            await UniTask.WhenAll(dependencySources);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            LoadStartedSignal.Dispatch(this);
            CurrentState = LoadItemState.Loading;
            if (_FakeLoadingTime)
                _fakeLoadingTimeTweener = DOTween.To(() => _currentLoadProgress, v => _currentLoadProgress = v, MAX_FAKE_PROGRESS, _FakeLoadingTimeDuration);

            if (IsValid)
            {
                await LoadContent(OnProgressUpdated);
            }
            else
            {
                DebugLite.LogError($"Load item '{name}' isn't valid!");
            }

            _fakeLoadingTimeTweener?.Kill();
            _fakeLoadingTimeTweener = null;
            LoadCompletedSignal.Dispatch(this);
            CurrentState = LoadItemState.Completed;
            CompletionSource.TrySetResult();

            LoadTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();
        }

        private void OnProgressUpdated(float progress)
        {
            if (_FakeLoadingTime && _fakeLoadingTimeTweener != null && !_fakeLoadingTimeTweener.IsComplete() && progress < MAX_FAKE_PROGRESS)
            {
                var remainingTime = _FakeLoadingTimeDuration - _fakeLoadingTimeTweener.Elapsed();
                _fakeLoadingTimeTweener.Kill();
                _currentLoadProgress = Mathf.Max(progress, _currentLoadProgress);
                _fakeLoadingTimeTweener = DOTween.To(() => _currentLoadProgress, v => _currentLoadProgress = v, MAX_FAKE_PROGRESS, remainingTime).SetEase(Ease.Linear);
            }
            else
            {
                _currentLoadProgress = Mathf.Max(progress, _currentLoadProgress);
            }
        }

        public async UniTask LoadContent(Action<float> progressCallback)
        {
            if (!IsValid)
                return;

            try
            {
                await _Source.LoadContent(this, progressCallback);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public float GetCurrentLoadingWeight() => GetLoadingProgress() * LoadWeight;

        public float GetLoadingProgress() => IsLoaded ? 1 : Mathf.Clamp(GetLoadingProgressRaw(), 0, 0.99f);

        protected virtual float GetLoadingProgressRaw() => _currentLoadProgress;

        public string GetLoadingDetail()
        {
            if (!IsValid)
                return $"{"Invalid".Color(ColorTools.Red)} - {UniqueID}";

            var progress = GetLoadingProgress();
            var progressText = $"{progress * 100:00}%".PadLeft(4);
            return $"{CurrentState.GetStateRichSprite()} {(IsLoaded ? progressText.Color(ColorTools.Lime) : progressText)} - {UniqueID}";
        }

        internal IEnumerable<LoadItemDefinition> GetSortingDependencies(DependencyResolutionContext context)
        {
            var ctx = context ?? _runtimeContext;
            var direct = ResolveDirectDependencies(ctx);

            if (SortingPreparedGroup is null)
            {
                DebugLite.LogError($"Load item '{name} has no prepared sorting group, is it assigned to a group?']");
                return direct;
            }

            return direct.Concat(SortingPreparedGroup.Dependencies.SelectMany(groupDependency => groupDependency.LoadItems));
        }

    #region Migration
        [HideInInspector]
        [SerializeField]
        private List<LoadItemDefinition> _Dependencies = new();

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize()
        {
            TryMigrate();
        }

        protected virtual void TryMigrate()
        {
            if (_Dependencies == null || _Dependencies.Count == 0)
                return;

            _ManualDependencies ??= new List<LoadItemDependency>();
            foreach (var legacyDependency in _Dependencies)
            {
                if (legacyDependency != null)
                    _ManualDependencies.Add(new LoadItemDependency(legacyDependency));
            }
            _Dependencies.Clear();
        }
    #endregion

#if UNITY_EDITOR
        public IEnumerable<Type> GetTypeDependencies() => _Source?.GetTypeDependencies() ?? Enumerable.Empty<Type>();
        public IEnumerable<Type> GetRepresentedTypes() => _Source?.GetRepresentedTypes() ?? Enumerable.Empty<Type>();

        [UsedImplicitly]
        private void OnSourceChanged(object prevValue, object newValue)
        {
#if ADDRESSABLES
            if (prevValue is PrefabLoadItemSource prefabSource && newValue is PrefabReferenceLoadItemSource newReferenceSource)
            {
                var prefabReference = AddressablesEditorUtils.CreateReference(prefabSource.Prefab);
                newReferenceSource.PrefabReference = prefabReference;
            }

            if (prevValue is PrefabReferenceLoadItemSource referenceSource && newValue is PrefabLoadItemSource newPrefabSource)
            {
                var prefab = referenceSource.PrefabReference.editorAsset;
                newPrefabSource.Prefab = prefab;
            }
#endif
        }
        
        private LoadItemDefinition DecoratedLoadItemDrawer(LoadItemDefinition value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            return LoaderUtils.DecoratedLoadItemDrawer(value, label, callNextDrawer);
        }
        
        protected override string FormatFileNameForID(string filename)
        {
            string[] suffixesToRemove = { "Definition", "LoadItem", "Loader" };
            foreach (var suffixToRemove in suffixesToRemove)
            {
                if (filename.EndsWith(suffixToRemove))
                    filename = filename[..^suffixToRemove.Length];
            }
            return filename;
        }

        [UsedImplicitly]
        private void ValidateSource(SelfValidationResult result, InspectorProperty property)
        {
            if (!IsValid) 
                result.AddError("Invalid source");
        }
        
        [UsedImplicitly]
        private void ValidateResolvedData(SelfValidationResult result, InspectorProperty property)
        {
            var (_, _, stale) = ComputeResolvedDataDiff();
            if (!stale) return;

            result.AddError("Resolved data are outdated")
                .WithFix(() =>
                {
                    RefreshResolvedData();
                    property.ForceMarkDirty();
                });
        }

        [Button]
        [EnhancedBoxGroup("Dependencies")]
        public void RefreshResolvedData()
        {
            var (newRepresented, newTypeDeps, _) = ComputeResolvedDataDiff();

            _RepresentedTypes.Clear();
            foreach (var t in newRepresented)
                _RepresentedTypes.Add(new UType(t));

            _ResolvedDependencies.Clear();
            foreach (var t in newTypeDeps)
                _ResolvedDependencies.Add(new TypedDependency(t));

            UnityEditor.EditorUtility.SetDirty(this);
        }

        private (List<Type> Represented, List<Type> TypeDeps, bool Stale) ComputeResolvedDataDiff()
        {
            var newRepresented = (_Source?.GetRepresentedTypes() ?? Enumerable.Empty<Type>())
                .Where(t => t != null)
                .Distinct()
                .ToList();

            var newTypeDeps = (_Source?.GetTypeDependencies() ?? Enumerable.Empty<Type>())
                .Where(t => t != null && !newRepresented.Any(t.IsAssignableFrom))
                .Distinct()
                .ToList();

            var existingRepresented = _RepresentedTypes.Select(ut => ut?.Value);
            var existingTypeDeps = _ResolvedDependencies.Select(td => td?.Type);

            var stale = !existingRepresented.SequenceEqual(newRepresented)
                     || !existingTypeDeps.SequenceEqual(newTypeDeps);

            return (newRepresented, newTypeDeps, stale);
        }

        [UsedImplicitly]
        private void ValidateDependencies(SelfValidationResult result, InspectorProperty property)
        {
            var ctx = DependencyResolutionContext.EditorContext;

            var unresolvedTypes = new List<string>();
            foreach (var typed in _ResolvedDependencies)
            {
                if (typed?.Type == null) continue;
                if (!ctx.GetItemsRepresentingType(typed.Type).Any())
                    unresolvedTypes.Add(typed.Type.Name);
            }
            if (unresolvedTypes.Count > 0)
                result.AddError($"No load item provides the required type(s): {string.Join(", ", unresolvedTypes)}");
            
            var selfReferences = new List<string>();
            var duplicates = new List<string>();
            var seenManual = new HashSet<LoadItemDefinition>();
            for (var i = 0; i < _ManualDependencies.Count; i++)
            {
                var record = _ManualDependencies[i];
                if (record == null) 
                    continue;
                
                if (record.Definition == this)
                    selfReferences.Add($"#{i}");
                else if (!seenManual.Add(record.Definition))
                    duplicates.Add(record.Definition.name);
            }
            
            if (selfReferences.Count > 0)
                result.AddError($"Self referencing manual dependency slot(s) {string.Join(", ", selfReferences)}");
            
            if (duplicates.Count > 0)
                result.AddError($"Duplicate manual dependencies: {string.Join(", ", duplicates.Distinct())}");

            var visited = new HashSet<LoadItemDefinition>();
            var path = new List<LoadItemDefinition>();
            var cycle = DetectCycle(this, this, visited, path, ctx);
            if (cycle != null && cycle.Count > 0)
                result.AddError($"Cyclic dependency detected: {string.Join(" → ", cycle.Append(this).Select(node => node.name))}");
        }
        
        private List<LoadItemDefinition> DetectCycle(
            LoadItemDefinition node, LoadItemDefinition root, HashSet<LoadItemDefinition> visited, List<LoadItemDefinition> path, DependencyResolutionContext context)
        {
            if (node == null)
                return null;

            visited.Add(node);
            path.Add(node);

            foreach (var dependency in node.EnumerateEditorPossibleDirectDependencies(context))
            {
                if (!visited.Contains(dependency))
                {
                    var cycle = DetectCycle(dependency, root, visited, path, context);
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
        
    #region Dependency Previews
        [EnhancedBoxGroup("Dependencies")]
        [PropertySpace]
        [Searchable]
        [ShowInInspector]
        [PropertyOrder(10)]
        [CustomValueDrawer("DecoratedLoadItemDrawer")]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = "TransitiveDependenciesTitleBarGUI")]
        public List<LoadItemDefinition> TransitiveDependencies { get; private set; }
        
        [EnhancedBoxGroup("Dependencies")]
        [ShowInInspector]
        [Searchable]
        [PropertyOrder(10)]
        [CustomValueDrawer("DecoratedLoadItemDrawer")]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = "DependentByTitleBarGUI")]
        public List<LoadItemDefinition> DependentBy { get; private set; }
              
        [UsedImplicitly]
        private void TransitiveDependenciesTitleBarGUI()
        {
            if (SirenixEditorGUI.ToolbarButton(EditorIcons.Refresh))
            {
                ResolveTransitiveDependencies();
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
        public void ResolveTransitiveDependencies()
        {
            var ctx = DependencyResolutionContext.EditorContext;
            var transitiveDependencies = new HashSet<LoadItemDefinition>();

            var queue = new Queue<LoadItemDefinition>(EnumerateEditorPossibleDirectDependencies(ctx));
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == null || current == this || !transitiveDependencies.Add(current))
                    continue;

                foreach (var dependency in current.EnumerateEditorPossibleDirectDependencies(ctx))
                {
                    if (dependency == null)
                        continue;
                    if (transitiveDependencies.Contains(dependency))
                        continue;
                    queue.Enqueue(dependency);
                }
            }

            TransitiveDependencies ??= new List<LoadItemDefinition>();
            TransitiveDependencies.Clear();
            TransitiveDependencies.AddRange(transitiveDependencies);
        }

        [OnInspectorInit]
        public void ResolveDependentBy()
        {
            DependentBy ??= new List<LoadItemDefinition>();
            DependentBy.Clear();
            var ctx = DependencyResolutionContext.EditorContext;
            DependentBy.AddRange(ctx.AllItems.Where(i => i != this && i.EnumerateEditorPossibleDirectDependencies(ctx).Contains(this)));
        }
        
        internal IEnumerable<LoadItemDefinition> EnumerateEditorPossibleDirectDependencies(DependencyResolutionContext context)
        {
            var seen = new HashSet<LoadItemDefinition>();
            foreach (var resolvedDependency in _ResolvedDependencies)
            {
                if (resolvedDependency == null)
                    continue;

                foreach (var item in resolvedDependency.Resolve(context))
                {
                    if (item != null && item != this && seen.Add(item)) 
                        yield return item;
                }
            }

            foreach (var manualDependency in _ManualDependencies)
            {
                if (manualDependency == null)
                    continue;
                
                var def = manualDependency.Definition;
                if (def != null && def != this && seen.Add(def))
                    yield return def;
            }
        }
    #endregion
#endif
    }
}
