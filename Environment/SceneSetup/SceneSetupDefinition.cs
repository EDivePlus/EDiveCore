// Author: František Holubec
// Created: 27.08.2025

using System.Collections.Generic;
using System.Linq;
using EDIVE.AssetTranslation;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.Presets;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    public class SceneSetupDefinition : AUniqueDefinition
    {
        [SceneReference]
        [SerializeField]
        [ListDrawerSettings(ShowFoldout = false)]
        private List<string> _Scenes;
        
        [SerializeField]
        private VisualPreset _Visual;
        
        [SerializeField]
        private bool _SetFirstSceneActive = true;
        
        public IEnumerable<string> Scenes => _Scenes.Where(s => !string.IsNullOrEmpty(s));
        public VisualPreset Visual => _Visual;
        public bool SetFirstSceneActive => _SetFirstSceneActive;
    }
}
