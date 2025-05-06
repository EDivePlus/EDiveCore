// Author: František Holubec
// Created: 21.10.2021

using System;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.Utils.Coloring
{
    [Serializable]
    [InlineProperty]
    public class ColorPalettePicker
    {
        [FormerlySerializedAs("palette")]
        [SerializeField]
        [EnhancedInlineEditor]
        [ShowCreateNew]
        [HideLabel]
        [BoxGroup("Border", false)]
        private AColorPalette _Palette;

        [FormerlySerializedAs("colorIndex")]
        [HorizontalGroup("Border/Color", 100)]
        [SerializeField]
        [LabelWidth(80)]
        [MinValue(0)]
        [MaxValue(nameof(ColorCount))]
        private int _ColorIndex;
        
        [HorizontalGroup("Border/Color")]
        [ShowInInspector]
        [HideLabel]
        public Color Color => _Palette ? _Palette.GetColor(_ColorIndex) : Color.clear;

        private int ColorCount => _Palette ? _Palette.ColorCount : 0;
        
        public static implicit operator Color(ColorPalettePicker picker)
        {
            return picker.Color;
        }
    }
}
