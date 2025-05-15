using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using EDIVE.AppLoading.Utils;
using EDIVE.AssetTranslation;
using EDIVE.External.Signals;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using EDIVE.EditorUtils;

using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Validation;
using Sirenix.Utilities.Editor;
#endif

namespace EDIVE.AppLoading.LoadItems
{
    public abstract class ALoadItemDefinition : AUniqueDefinition, IComparable<ALoadItemDefinition>
    {
        [SerializeField]
        private float _LoadWeight = 1f;

        [SerializeField]
        private bool _FakeLoadingTime;

        [SerializeField]
        [SuffixLabel("s", true)]
        [ShowIf(nameof(_FakeLoadingTime))]
        private float _FakeLoadingTimeDuration = 2;

        [PropertySpace]
        [Searchable]
        [PropertyOrder(50)]
        [SerializeField]
        [CustomValueDrawer("DecoratedLoadItemDrawer")]
        [EnhancedValidate("ValidateDependencies")]
        private List<ALoadItemDefinition> _Dependencies = new();

        public float LoadWeight => _LoadWeight;

        [EnhancedValidate("ValidateDefinition")]
        [ShowInInspector] [ReadOnly]
        public abstract bool IsValid { get; }

        public UniTaskCompletionSource CompletionSource { get; private set; }

        [ShowInInspector]
        [ReadOnly]
        public LoadItemState CurrentState { get; private set; } = LoadItemState.Undefined;

        public bool IsLoaded => CurrentState == LoadItemState.Completed;
        public List<ALoadItemDefinition> Dependencies => _Dependencies;

        public float LoadTime { get; private set; }
        public Signal<ALoadItemDefinition> LoadStartedSignal { get; } = new();
        public Signal<ALoadItemDefinition> LoadCompletedSignal { get; } = new();

        private Tweener _fakeLoadingTimeTweener;
        private float _currentLoadProgress;

        internal LoadGroupDefinition SortingPreparedGroup;

        private const float MAX_FAKE_PROGRESS = 0.9f;

        public void Initialize()
        {
            CompletionSource = new UniTaskCompletionSource();
            CurrentState = LoadItemState.Pending;
            _currentLoadProgress = 0f;
        }

        public void Terminate()
        {
            CompletionSource = null;
        }

        public int CompareTo(ALoadItemDefinition other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;

            if (_Dependencies.Contains(other)) return -1;
            if (other._Dependencies.Contains(this)) return 1;
            return 0;
        }

        public async UniTask Load()
        {
            // Wait for all dependencies to be loaded
            var dependencySources = _Dependencies.Where(l => l.IsValid).Select(d => d.CompletionSource.Task);
            await UniTask.WhenAll(dependencySources);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            // Start loading
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
                Debug.LogError($"Load item '{name}' isn't valid!");
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

        public abstract UniTask LoadContent(Action<float> progressCallback);

        public float GetCurrentLoadingWeight() { return GetLoadingProgress() * LoadWeight; }

        public float GetLoadingProgress() { return IsLoaded ? 1 : Mathf.Clamp(GetLoadingProgressRaw(), 0, 0.99f); }

        protected virtual float GetLoadingProgressRaw() { return _currentLoadProgress; }

        public string GetLoadingDetail()
        {
            if (!IsValid)
                return $"{"Invalid".Color(ColorTools.Red)} - {UniqueID}";

            var progress = GetLoadingProgress();
            var progressText = $"{progress * 100:00}%".PadLeft(4);
            return $"{CurrentState.GetStateRichSprite()} {(IsLoaded ? progressText.Color(ColorTools.Lime) : progressText)} - {UniqueID}";
        }

        internal IEnumerable<ALoadItemDefinition> GetSortingDependencies()
        {
            if (SortingPreparedGroup is null)
            {
                Debug.LogError($"Load item '{name} has no prepared sorting group, is it assigned to a group?']");
                return Dependencies;
            }

            return Dependencies.Concat(SortingPreparedGroup.Dependencies.SelectMany(groupDependency => groupDependency.LoadItems));
        }

#if UNITY_EDITOR
        [PropertySpace]
        [Searchable]
        [ShowInInspector]
        [PropertyOrder(50)]
        [ReadOnlyListElements]
        [CustomValueDrawer("DecoratedLoadItemDrawer")]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = "TransitiveDependenciesTitleBarGUI")]
        public List<ALoadItemDefinition> TransitiveDependencies { get; private set; }

        [ShowInInspector]
        [Searchable]
        [PropertyOrder(50)]
        [ReadOnlyListElements]
        [CustomValueDrawer("DecoratedLoadItemDrawer")]
        [ListDrawerSettings(IsReadOnly = true, OnTitleBarGUI = "DependentByTitleBarGUI")]
        public List<ALoadItemDefinition> DependentBy { get; private set; }

        public abstract IEnumerable<Type> GetTypeDependencies();
        public abstract IEnumerable<Type> GetRepresentedTypes();

        /*
        protected override void PlayModeStarted()
        {
            base.PlayModeStarted();
            CurrentState = LoadItemState.Undefined;
        }
        */

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
            DependentBy ??= new List<ALoadItemDefinition>();
            DependentBy.Clear();
            DependentBy.AddRange(EditorAssetUtils.FindAllAssetsOfType<ALoadItemDefinition>().Where(i => i._Dependencies.Contains(this)));
        }

        [OnInspectorInit]
        public void ResolveCompleteDependencies()
        {
            var transitiveDependencies = new HashSet<ALoadItemDefinition>();

            var queue = new Queue<ALoadItemDefinition>(_Dependencies);
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

            TransitiveDependencies ??= new List<ALoadItemDefinition>();
            TransitiveDependencies.Clear();
            TransitiveDependencies.AddRange(transitiveDependencies);
        }

        [UsedImplicitly]
        private void ValidateDependencies(SelfValidationResult result, InspectorProperty property)
        {
            var visited = new HashSet<ALoadItemDefinition>();
            var path = new List<ALoadItemDefinition>();

            var cycle = DetectCycle(this, this, visited, path);
            if (cycle != null && cycle.Count > 0)
            {
                result.AddError($"Cyclic dependency detected: {string.Join("-", cycle.Select(node => node.name))}");
            }

            var providedTypes = _Dependencies.Where(d => d != null).SelectMany(d => d.GetRepresentedTypes()).ToList();
            var missingTypes = GetTypeDependencies().Where(requiredType => !providedTypes.Any(requiredType.IsAssignableFrom)).ToList();

            if (missingTypes.Count > 0)
            {
                result.AddError($"Missing dependencies: {string.Join(", ", missingTypes.Select(t => t.Name))}")
                    .WithFix(() =>
                    {
                        var foundItems = LoaderUtils.FindLoadItems(missingTypes);
                        foreach (var foundItem in foundItems)
                        {
                            if (_Dependencies.Contains(foundItem)) continue;
                            _Dependencies.Add(foundItem);
                        }

                        property.ForceMarkDirty();
                    });
            }

            var invalidDependencies = _Dependencies.Where(d => d == null || !d.IsValid).ToList();
            if (invalidDependencies.Count > 0)
            {
                result.AddError($"Invalid dependencies: {string.Join(", ", invalidDependencies.Select(d => d != null ? d.name : "Null"))}");
            }
        }

        [UsedImplicitly]
        private void ValidateDefinition(SelfValidationResult result, InspectorProperty property)
        {
            if (!IsValid)
            {
                result.AddError("Invalid definition");
            }
        }

        private List<ALoadItemDefinition> DetectCycle(ALoadItemDefinition node, ALoadItemDefinition root, HashSet<ALoadItemDefinition> visited, List<ALoadItemDefinition> path)
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

        private ALoadItemDefinition DecoratedLoadItemDrawer(ALoadItemDefinition value, GUIContent label, Func<GUIContent, bool> callNextDrawer)
        {
            return LoaderUtils.DecoratedLoadItemDrawer(value, label, callNextDrawer);
        }

        protected override string FormatFileNameForID(string filename) => filename.Replace("Definition", "");
#endif
    }
}
