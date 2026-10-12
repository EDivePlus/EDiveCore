// Author: František Holubec
// Created: 12.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.Utils.Activations;
using EDIVE.View.ViewTree;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.View.Windows
{
    // Shows one page at a time, the others are hidden. Pages are child groups in the view tree.
    public class ViewSwitch : ViewGroup
    {
        [Serializable]
        public class Page
        {
            // Unique in its window, used by Navigate paths
            [Required]
            public string ID;

            [Required]
            public ViewGroup Group;

            // Selects this page, e.g. tab button
            [SerializeReference]
            public IActivation Activation;

            // On while selected, e.g. tab button look
            public AToggleState Toggle;
        }

        // First page when empty
        [SerializeField]
        [EnhancedBoxGroup("Switch")]
        [EnhancedValueDropdown(nameof(GetPageIDs))]
        private string _DefaultPage;

        [SerializeField]
        [EnhancedBoxGroup("Switch")]
        [EnhancedValidate(nameof(ValidatePages))]
        private List<Page> _Pages = new();

        private int _selected = -1;
        private readonly DisposableGroup _activationListeners = new();

        public IReadOnlyList<Page> Pages => _Pages;
        public Page Selected => _selected >= 0 && _selected < _Pages.Count ? _Pages[_selected] : null;

        public event Action<ViewSwitch> SelectionChanged;

        protected override void OnEnable()
        {
            EnsureSelected();
            ApplyToggles(true);
            for (var i = 0; i < _Pages.Count; i++)
            {
                var index = i;
                _activationListeners.Add(_Pages[i].Activation.RegisterDisposable(() => OnPageActivated(index)));
            }
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            _activationListeners.Clear();
            base.OnDisable();
        }

        public int IndexOf(string id)
        {
            for (var i = 0; i < _Pages.Count; i++)
            {
                if (string.Equals(_Pages[i].ID, id, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        public bool Select(string id)
        {
            var index = IndexOf(id);
            if (index < 0)
                return false;

            Select(index);
            return true;
        }

        public void Select(int index)
        {
            if (index < 0 || index >= _Pages.Count)
                return;

            EnsureSelected();
            if (index == _selected)
                return;

            _selected = index;
            RefreshChildCaps();
            ApplyToggles(false);
            SelectionChanged?.Invoke(this);
        }

        // Each segment selects a page in the nearest switches below the previous page, e.g. "audio/advanced"
        public static bool Navigate(ViewGroup scope, string path)
        {
            if (scope == null || string.IsNullOrEmpty(path))
                return false;

            foreach (var segment in path.Split('/'))
            {
                if (!TrySelectBelow(scope, segment, out var page))
                {
                    Debug.LogWarning($"[ViewSwitch] No page '{segment}' below {scope.name} for path '{path}'.", scope);
                    return false;
                }
                scope = page;
            }
            return true;
        }

        // Pages of nested switches belong to the next segment
        private static bool TrySelectBelow(ViewGroup scope, string id, out ViewGroup page)
        {
            if (scope is ViewSwitch scopeSwitch && scopeSwitch.TrySelect(id, out page))
                return true;

            foreach (var child in scope.Children)
            {
                if (child is ViewSwitch childSwitch)
                {
                    if (childSwitch.TrySelect(id, out page))
                        return true;
                    continue;
                }

                if (child is ViewGroup group && TrySelectBelow(group, id, out page))
                    return true;
            }

            page = null;
            return false;
        }

        private bool TrySelect(string id, out ViewGroup page)
        {
            var index = IndexOf(id);
            page = index >= 0 ? _Pages[index].Group : null;
            if (page == null)
                return false;

            Select(index);
            return true;
        }

        protected override ViewState GetChildCap(AViewNode child)
        {
            EnsureSelected();
            for (var i = 0; i < _Pages.Count; i++)
            {
                if (_Pages[i].Group == child)
                    return i == _selected ? ViewState.Focused : ViewState.Hidden;
            }
            return ViewState.Focused;
        }

        private void EnsureSelected()
        {
            if (_selected >= 0 || _Pages.Count == 0)
                return;

            _selected = Mathf.Max(0, IndexOf(_DefaultPage));
        }

        private void ApplyToggles(bool immediate)
        {
            for (var i = 0; i < _Pages.Count; i++)
            {
                var toggle = _Pages[i].Toggle;
                if (toggle != null)
                    toggle.SetState(i == _selected, immediate);
            }
        }

        private void OnPageActivated(int index)
        {
            if (FocusedInTree)
                Select(index);
        }

#if UNITY_EDITOR
        private IEnumerable<string> GetPageIDs() => _Pages.Where(p => !string.IsNullOrEmpty(p.ID)).Select(p => p.ID);

        private void ValidatePages(SelfValidationResult result)
        {
            var ids = new HashSet<string>();
            foreach (var page in _Pages)
            {
                if (page == null)
                    continue;

                if (!string.IsNullOrEmpty(page.ID) && !ids.Add(page.ID))
                    result.AddError($"Page ID '{page.ID}' is used twice.");

                if (page.Group != null && page.Group.GetDesignatedParent() != this)
                    result.AddError($"Page {page.Group.name} is not a child of this switch in the view tree.");
            }
        }
#endif
    }
}
