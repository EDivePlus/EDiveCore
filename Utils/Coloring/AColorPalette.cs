// Author: František Holubec
// Created: 21.10.2021

using UnityEngine;

namespace EDIVE.Utils.Coloring
{
    public abstract class AColorPalette : ScriptableObject
    {
        public abstract int ColorCount { get; }
        public abstract Color GetColor(int index);
    }
}
