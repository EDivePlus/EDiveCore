// Author: František Holubec
// Created: 21.10.2021

using System;
using EDIVE.OdinExtensions;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace EDIVE.Utils.Coloring
{
    [Serializable]
    [InlineProperty]
    public class ColorPalettePicker
    {
        [FormerlySerializedAs("palette")]
        [HorizontalGroup("Row")]
        [HideLabel]
        [SerializeField]
        private AColorPalette _Palette;

        [FormerlySerializedAs("colorIndex")]
        [HorizontalGroup("Row", 50)]
        [HideLabel]
        [SerializeField]
        private int _ColorIndex;

        public ColorPalettePicker() { }
        public ColorPalettePicker(AColorPalette palette, int colorIndex)
        {
            _Palette = palette;
            _ColorIndex = colorIndex;
        }

        public AColorPalette Palette => _Palette;
        public int ColorIndex => _ColorIndex;

        [HorizontalGroup("Row", 80)]
        [HideLabel]
        [ShowInInspector]
        [InlineIconButton(FontAwesomeEditorIconType.PaletteSolid, nameof(ShowSwatches), "Pick color", GUIAlwaysEnabled = true)]
        public Color Color => _Palette ? _Palette.GetColor(_ColorIndex) : Color.clear;

        public static implicit operator Color(ColorPalettePicker picker)
        {
            return picker.Color;
        }

#if UNITY_EDITOR
        private void ShowSwatches(Rect fieldRect, InspectorProperty property)
        {
            if (_Palette == null)
                return;

            var indexEntry = property.Parent.Children[nameof(_ColorIndex)].ValueEntry;
            ColorPaletteGUI.ShowSwatchesPopup(fieldRect, _Palette, _ColorIndex, index =>
            {
                property.Tree.DelayActionUntilRepaint(() => indexEntry.WeakSmartValue = index);
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            });
        }
#endif
    }
}
