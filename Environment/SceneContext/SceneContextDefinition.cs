// Author: František Holubec
// Created: 27.08.2025

using System.Collections.Generic;
using System.Linq;
using EDIVE.Environment.Lighting;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;

namespace EDIVE.Environment.SceneContext
{
    public class SceneContextDefinition : ScriptableObject
    {
        [SerializeField]
        private SceneLightingConfig _Lighting;
        
        [SceneReference]
        [SerializeField]
        private List<string> _Scenes;
        
        public SceneLightingConfig Lighting => _Lighting;
        public IEnumerable<string> Scenes => _Scenes.Where(s => !string.IsNullOrEmpty(s));
        public ASceneContextSpawnPlace SpawnPlace {get; private set; }
        
        public void RegisterSpawnPlace(ASceneContextSpawnPlace spawnPlace)
        {
            SpawnPlace = spawnPlace;
        }
    }
}
