// Author: František Holubec
// Created: 13.10.2026

using System.Collections.Generic;
using System.Linq;
using EDIVE.VisualPresets.VisualIDs;
using UnityEngine;

namespace EDIVE.VisualPresets.Presets
{
    // Later sources win for the same ID
    [CreateAssetMenu(menuName = "EDIVE/Visual Presets/Visual Preset Stack")]
    public class VisualPresetStack : AVisualPresetSource
    {
        [SerializeField]
        private List<AVisualPresetSource> _Sources = new();

        private readonly List<AVisualPresetSource> _subscribed = new();
        private VisualPreset _resolved;
        private bool _resolving;
        private bool _notifying;

        public IReadOnlyList<AVisualPresetSource> Sources => _Sources;

        public override VisualPreset Preset => _resolved ??= Resolve();

        public void SetSources(IEnumerable<AVisualPresetSource> sources)
        {
            _Sources = sources?.ToList() ?? new List<AVisualPresetSource>();
            Refresh();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void OnValidate() => Refresh();

        private void Refresh()
        {
            Unsubscribe();
            Subscribe();
            OnSourceChanged();
        }

        private void Subscribe()
        {
            foreach (var source in _Sources.Where(s => s != null && s != this && !_subscribed.Contains(s)))
            {
                source.Changed += OnSourceChanged;
                _subscribed.Add(source);
            }
        }

        private void Unsubscribe()
        {
            foreach (var source in _subscribed.Where(s => s != null))
            {
                source.Changed -= OnSourceChanged;
            }
            _subscribed.Clear();
        }

        private void OnSourceChanged()
        {
            if (_notifying)
                return;

            _resolved = null;
            _notifying = true;
            try
            {
                InvokeChanged();
            }
            finally
            {
                _notifying = false;
            }
        }

        private VisualPreset Resolve()
        {
            if (_resolving)
            {
                Debug.LogWarning($"[VisualPresetStack] {name} includes itself", this);
                return new VisualPreset();
            }

            _resolving = true;
            try
            {
                var records = new List<AVisualPresetRecord>();
                var indices = new Dictionary<ABaseVisualID, int>();
                foreach (var source in _Sources.Where(s => s != null && s != this && s.Preset != null))
                {
                    foreach (var record in source.Preset.EnumerateValidRecords())
                    {
                        if (indices.TryGetValue(record.BaseVisualID, out var index))
                        {
                            records[index] = record;
                        }
                        else
                        {
                            indices[record.BaseVisualID] = records.Count;
                            records.Add(record);
                        }
                    }
                }
                return new VisualPreset(records);
            }
            finally
            {
                _resolving = false;
            }
        }
    }
}
