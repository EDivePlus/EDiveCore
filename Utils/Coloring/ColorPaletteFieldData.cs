// Author: František Holubec
// Created: 14.10.2026

using System;
using EDIVE.DataStructures.VariableFields;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils.Coloring
{
    // Read only, the color comes from the palette
    [Serializable]
    [TypeRegistryItem("Color Palette")]
    public class ColorPaletteFieldData : IVariableFieldData<Color>
    {
        [HideLabel]
        [SerializeField]
        private ColorPalettePicker _Picker = new();

        public Color Value
        {
            get => _Picker.Color;
            set { }
        }
    }
}
