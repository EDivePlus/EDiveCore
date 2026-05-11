// Author: František Holubec
// Created: 07.05.2026

using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Utils.DefinesRegistry
{
    public class ActiveDefinesAsset : ScriptableObject
    {
        [Searchable]
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false, NumberOfItemsPerPage = 30)]
        private List<string> _Defines = new();

        public IReadOnlyList<string> Defines => _Defines;

#if UNITY_EDITOR
        internal void SetDefines(IEnumerable<string> defines)
        {
            _Defines.Clear();
            _Defines.AddRange(defines);
        }
#endif
    }
}
