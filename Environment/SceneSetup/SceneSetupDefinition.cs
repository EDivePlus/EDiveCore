// Author: František Holubec
// Created: 27.08.2025

using System.Collections.Generic;
using System.Linq;
using EDIVE.Environment.Sky;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    public class SceneSetupDefinition : ScriptableObject
    {
        [SerializeField]
        private SkyDefinition _Sky;
        
        [SceneReference]
        [SerializeField]
        private List<string> _Scenes;
        
        public SkyDefinition Sky => _Sky;
        public IEnumerable<string> Scenes => _Scenes.Where(s => !string.IsNullOrEmpty(s));
        public ASceneSpawnPlace SpawnPlace {get; private set; }
        
        public void RegisterSpawnPlace(ASceneSpawnPlace spawnPlace)
        {
            SpawnPlace = spawnPlace;
        }
    }
}
