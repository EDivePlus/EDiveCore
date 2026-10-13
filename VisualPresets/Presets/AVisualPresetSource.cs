// Author: František Holubec
// Created: 13.10.2026

using System;
using EDIVE.VisualPresets.VisualIDs;
using UnityEngine;

namespace EDIVE.VisualPresets.Presets
{
    // Shared visual values, e.g. a UI style
    public abstract class AVisualPresetSource : ScriptableObject
    {
        public event Action Changed;

        public abstract VisualPreset Preset { get; }

        public bool TryGetRecord<TRecord>(ABaseVisualID visualID, out TRecord record) where TRecord : AVisualPresetRecord
        {
            var preset = Preset;
            if (visualID != null && preset != null)
            {
                foreach (var presetRecord in preset.EnumerateValidRecords())
                {
                    if (presetRecord.BaseVisualID != visualID || presetRecord is not TRecord typedRecord)
                        continue;

                    record = typedRecord;
                    return true;
                }
            }

            record = null;
            return false;
        }

        protected void InvokeChanged() => Changed?.Invoke();
    }
}
