// Author: František Holubec
// Created: 10.06.2025

using System.Collections.Generic;
using System.Linq;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils.FontSymbols
{
    public class CodepointsAsset : ScriptableObject
    {
        [EnableGUI]
        [ReadOnly]
        [EnhancedTableList]
        [SerializeField]
        private List<Codepoint> _Entries = new();
        public List<Codepoint> Entries => _Entries;

        public uint[] Unicodes => _Entries.Select(e => e.Unicode).ToArray();

        public bool TryGetCodepoint(char symbol, out Codepoint codepoint)
        {
            return _Entries.TryGetFirst(t => t.Char == symbol, out codepoint);
        }
    }
}
