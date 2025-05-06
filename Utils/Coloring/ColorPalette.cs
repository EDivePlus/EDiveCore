// Author: František Holubec
// Created: 21.10.2021

using System.Collections.Generic;
using EDIVE.NativeUtils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.Utils.Coloring
{
    public class ColorPalette : AColorPalette
    {
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false, ShowIndexLabels = true)]
        private List<Color> _Colors;

        public override int ColorCount => _Colors.Count;
        
        public override Color GetColor(int index)
        {
            if(_Colors.Count == 0) return Color.clear;
            var newIndex = index.PositiveModulo(_Colors.Count);
            return _Colors[newIndex];
        }
    }
}
