// Author: František Holubec
// Created: 13.10.2026

using System;
using EDIVE.DataStructures.VariableFields;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets.Presets
{
    // Read only, the color comes from the source, e.g. the UI style
    [Serializable]
    [TypeRegistryItem("Visual Preset")]
    public class ColorVisualPresetFieldData : IVariableFieldData<Color>
    {
        private static readonly Color MISSING_COLOR = Color.magenta;

        [HorizontalGroup]
        [HideLabel]
        [SerializeField]
        private AVisualPresetSource _Source;

        [HorizontalGroup]
        [HideLabel]
        [SerializeField]
        private ColorVisualID _ColorID;

        [NonSerialized]
        private bool _warned;

        public Color Value
        {
            get
            {
                if (_Source != null && _Source.TryGetRecord<ColorVisualPresetRecord>(_ColorID, out var record))
                    return record.Color;

                if (_warned)
                    return MISSING_COLOR;

                _warned = true;
                Debug.LogWarning($"[ColorVisualPresetFieldData] No color '{(_ColorID != null ? _ColorID.name : "None")}' in '{(_Source != null ? _Source.name : "None")}'");
                return MISSING_COLOR;
            }
            set { }
        }
    }
}
