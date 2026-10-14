// Author: František Holubec
// Created: 21.10.2021

using System;
using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EDIVE.Utils.Coloring
{
    public class ColorPalette : AColorPalette
    {
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false, ShowIndexLabels = true)]
        private List<Color> _Colors = new();

        [HideLabel]
        [InlineProperty]
        [EnhancedBoxGroup("Generator")]
        [SerializeField]
        private ColorPaletteGenerator _Generator = new();

        public override int ColorCount => _Colors.Count;

        public override Color GetColor(int index)
        {
            return index >= 0 && index < _Colors.Count ? _Colors[index] : _Generator.GetColor(index);
        }

#if UNITY_EDITOR
        private const float APPLY_BUTTON_WIDTH = 60f;

        [OnInspectorGUI]
        [PropertyOrder(-1)]
        private void DrawPreview()
        {
            ColorPaletteGUI.DrawSwatches(this, -1, 4);
        }

        [OnInspectorGUI]
        [EnhancedBoxGroup("Generator", SpaceBefore = 4)]
        [PropertyOrder(1)]
        private void DrawGeneratorPreview()
        {
            var generated = _Generator.Generate().ToList();
            EditorGUILayout.BeginHorizontal();
            ColorPaletteGUI.DrawSwatches(generated, generatedFrom: _Generator.BaseHues, reservedWidth: APPLY_BUTTON_WIDTH);
            if (GUILayout.Button("Apply", GUILayout.Width(APPLY_BUTTON_WIDTH)))
            {
                Undo.RecordObject(this, "Generate Palette");
                _Colors = generated;
                EditorUtility.SetDirty(this);
            }
            EditorGUILayout.EndHorizontal();
        }
#endif
    }

    [Serializable]
    public class ColorPaletteGenerator
    {
        private const float GOLDEN_ANGLE = 137.50777f;

        [Range(0, 1)]
        [SerializeField]
        private float _Lightness = 0.85f;

        [Range(0, 0.37f)]
        [SerializeField]
        private float _Chroma = 0.11f;

        [Range(0, 360)]
        [SerializeField]
        private float _StartHue = 50f;

        // Fewer base hues keep the first colors further apart
        [MinValue(1)]
        [SerializeField]
        private int _BaseHues = 8;

        // Colors Apply writes to the palette
        [MinValue(1)]
        [SerializeField]
        private int _Count = 8;

        public int BaseHues => _BaseHues;

        public Color GetColor(int index)
        {
            var hue = index < _BaseHues
                ? _StartHue + index * 360f / _BaseHues
                : _StartHue + 180f / _BaseHues + (index - _BaseHues) * GOLDEN_ANGLE;
            return PerceptualColorUtility.ToColor(_Lightness, _Chroma, hue);
        }

        public IEnumerable<Color> Generate()
        {
            for (var i = 0; i < _Count; i++)
                yield return GetColor(i);
        }
    }
}
