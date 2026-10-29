// Author: František Holubec
// Created: 14.10.2026

using System.Collections.Generic;
using EDIVE.View.ViewTree;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using EDIVE.View.Windows.Screens;

namespace EDIVE.View.Windows.Buttons
{
    // Spawns window buttons for a catalog: all windows for the library, pinned and shown ones for the dock
    public class WindowButtonList : MonoBehaviour
    {
        public enum ListContent
        {
            All,
            // Pinned ones, then catalog windows that are shown but not pinned
            PinnedAndShown
        }

        [SerializeField]
        [Required]
        private WindowCatalog _Catalog;

        [SerializeField]
        [FormerlySerializedAs("_Content")]
        private ListContent _ListedWindows;

        [SerializeField]
        [Required]
        private WindowButton _ButtonPrefab;

        [Tooltip("Buttons go here, at the end of the existing children")]
        [SerializeField]
        [Required]
        private RectTransform _Container;

        [SerializeField]
        [Tooltip("Optional, shows how many buttons are listed")]
        private TMP_Text _CountText;

        private readonly List<WindowButton> _buttons = new();
        private readonly List<WindowDefinition> _wanted = new();
        private ScreenRoot _screen;
        private int _firstIndex;

        private void Start()
        {
            // Authored children stay in front
            _firstIndex = _Container.childCount;
            _screen = this.FindInViewTree<ScreenRoot>();
            if (_screen == null)
                Debug.LogError($"[WindowButtonList] {name} is not under a ScreenRoot.", this);
            else
                _screen.Changed += OnScreenChanged;
            Rebuild();
        }

        private void OnDestroy()
        {
            if (_screen != null)
                _screen.Changed -= OnScreenChanged;
        }

        private void OnScreenChanged(ScreenRoot screen)
        {
            if (_ListedWindows == ListContent.PinnedAndShown)
                Rebuild();
        }

        private void Rebuild()
        {
            CollectWanted();

            // Keeps buttons that stay, so their states don't restart
            for (var i = _buttons.Count - 1; i >= 0; i--)
            {
                if (_buttons[i] != null && _wanted.Contains(_buttons[i].Definition))
                    continue;

                if (_buttons[i] != null)
                    Destroy(_buttons[i].gameObject);
                _buttons.RemoveAt(i);
            }

            for (var i = 0; i < _wanted.Count; i++)
            {
                var button = _buttons.Find(b => b.Definition == _wanted[i]);
                if (button == null)
                {
                    button = Instantiate(_ButtonPrefab, _Container, false);
                    button.name = $"{_ButtonPrefab.name} {_wanted[i].name}";
                    button.Setup(_wanted[i], _screen);
                    _buttons.Add(button);
                }
                button.transform.SetSiblingIndex(_firstIndex + i);
            }
            if (_CountText)
                _CountText.text = _buttons.Count.ToString();
        }

        private void CollectWanted()
        {
            _wanted.Clear();
            if (_Catalog == null)
                return;

            if (_ListedWindows == ListContent.All)
            {
                foreach (var window in _Catalog.Windows)
                {
                    if (window != null)
                        _wanted.Add(window);
                }
                return;
            }

            foreach (var window in _Catalog.Pinned)
            {
                if (window != null)
                    _wanted.Add(window);
            }

            if (_screen == null)
                return;

            foreach (var handle in _screen.Handles)
            {
                if (handle.IsClosing || handle.IsCloseRequested || _wanted.Contains(handle.Definition) || !_Catalog.Contains(handle.Definition))
                    continue;
                _wanted.Add(handle.Definition);
            }
        }
    }
}
