// Author: Michal Petr
// Created: 29.10.2025

using System;
using EDIVE.VisualPresets.VisualIDs;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.VisualPresets.Presets
{
    [Serializable]
    public class PrefabVisualPresetRecord : AVisualPresetRecord<PrefabVisualID>
    {
        [VerticalGroup("Value")]
        [SerializeField]
        private GameObject _Prefab;
        
        public GameObject Prefab
        {
            get => _Prefab;
            set => _Prefab = value;
        }
    }
}
