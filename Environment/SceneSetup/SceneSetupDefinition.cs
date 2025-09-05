// Author: František Holubec
// Created: 27.08.2025

using System.Collections.Generic;
using System.Linq;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    public class SceneSetupDefinition : ScriptableObject
    {
        [SceneReference]
        [SerializeField]
        private List<string> _Scenes;
        
        [SerializeField]
        private bool _SetFirstSceneActive = true;
        
        public bool SetFirstSceneActive => _SetFirstSceneActive;
        public IEnumerable<string> Scenes => _Scenes.Where(s => !string.IsNullOrEmpty(s));
        public SceneSetupController Controller {get; private set; }
        
        public void RegisterController(SceneSetupController controller)
        {
            Controller = controller;
        }
    }
}
