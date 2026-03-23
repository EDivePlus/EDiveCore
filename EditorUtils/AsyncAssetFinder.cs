// Author: František Holubec
// Created: 23.03.2026

#if EDITOR_COROUTINES
using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.Utilities.Editor;
using Unity.EditorCoroutines.Editor;

namespace EDIVE.EditorUtils
{
    public class AsyncAssetsFinder<TElement> where TElement : UnityEngine.Object
    {
        private const double MAX_SEARCH_DURATION_PR_FRAME_IN_MS = 10;
        
        public int NumberOfResultsToSearch { get; private set; } = 0;
        public int CurrentSearchingIndex { get; private set; } = 0;

        public List<TElement> CurrentElements { get; private set; } = new List<TElement>();
        
        public bool IsSearching => _searchRoutine != null;
        
        private EditorCoroutine _searchRoutine;
        private readonly Action _onSearchCompletedAction;
        private readonly Predicate<TElement> _elementFilterPredicate;
        private readonly int _capacity;

        public AsyncAssetsFinder(Action onSearchCompletedAction = null, Predicate<TElement> elementFilterPredicate = null, int capacity = -1)
        {
            _onSearchCompletedAction = onSearchCompletedAction;
            _elementFilterPredicate = elementFilterPredicate;
            _capacity = capacity;
        }

        public void SearchAssetsAsync(bool force = false)
        {
            if (force)
            {
                EditorCoroutineUtility.StopCoroutine(_searchRoutine);
                _searchRoutine = null;
            }
            _searchRoutine ??= EditorCoroutineUtility.StartCoroutineOwnerless(SearchAssetsRoutine());
        }

        private IEnumerator SearchAssetsRoutine()
        {
            var seenObjects = new HashSet<TElement>();
            CurrentElements ??= new List<TElement>();
            CurrentElements.Clear();

            var allAssets = AssetUtilities.GetAllAssetsOfTypeWithProgress(typeof(TElement));

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            foreach (var p in allAssets)
            {
                if (sw.Elapsed.TotalMilliseconds > MAX_SEARCH_DURATION_PR_FRAME_IN_MS)
                {
                    NumberOfResultsToSearch = p.NumberOfResults;
                    CurrentSearchingIndex = p.CurrentIndex;
                    
                    yield return null;
                    sw.Reset();
                    sw.Start();
                }

                var asset = p.Asset as TElement;

                if (asset != null && FilterElement(asset) && seenObjects.Add(asset)) 
                    CurrentElements.Add(asset);

                if (_capacity > 0 && CurrentElements.Count >= _capacity)
                    break;
            }

            _onSearchCompletedAction?.Invoke();

            GUIHelper.RequestRepaint();
            yield return null;
            _searchRoutine = null;
        }

        private bool FilterElement(TElement element)
        {
            return _elementFilterPredicate == null || _elementFilterPredicate.Invoke(element);
        }
    }
}
#endif
