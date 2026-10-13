// Author: František Holubec
// Created: 13.10.2026

using System.Linq;
using EDIVE.VisualPresets.Presets;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.VisualPresets.Switchers
{
    // Applies a shared source, e.g. the UI style, and follows its changes
    [ExecuteAlways]
    public class VisualPresetApplier : MonoBehaviour
    {
        [Required]
        [SerializeField]
        private AVisualPresetSource _Source;

        [SerializeField]
        private VisualSwitcher _Switcher = new();

        private AVisualPresetSource _subscribed;

        public AVisualPresetSource Source
        {
            get => _Source;
            set
            {
                _Source = value;
                if (isActiveAndEnabled)
                    Refresh();
            }
        }

        private void OnEnable() => Refresh();
        private void OnDisable() => Unsubscribe();

        public void Apply()
        {
            if (_Source == null)
                return;

#if UNITY_EDITOR
            var before = Application.isPlaying ? null : _Switcher.Capture();
#endif
            _Switcher.Apply(_Source.Preset);
#if UNITY_EDITOR
            if (before != null && !before.EnumerateValidRecords().SequenceEqual(_Switcher.Capture().EnumerateValidRecords()))
                MarkTargetsDirty();
#endif
        }

        private void OnSourceChanged()
        {
            Apply();
            foreach (var refreshable in GetComponents<IVisualPresetRefreshable>())
                refreshable.RefreshVisuals();
        }

        private void Refresh()
        {
            if (_subscribed != _Source)
            {
                Unsubscribe();
                _subscribed = _Source;
                if (_subscribed != null)
                    _subscribed.Changed += OnSourceChanged;
            }
            Apply();
        }

        private void Unsubscribe()
        {
            if (_subscribed != null)
                _subscribed.Changed -= OnSourceChanged;
            _subscribed = null;
        }

#if UNITY_EDITOR
        // Keeps the last applied style in the file
        private void MarkTargetsDirty()
        {
            foreach (var target in _Switcher.GetTargets())
            {
                EditorUtility.SetDirty(target);
                if (PrefabUtility.IsPartOfPrefabInstance(target))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            }
        }
#endif
    }
}
