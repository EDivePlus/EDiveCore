// Author: František Holubec
// Created: 05.09.2025

using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    public class SceneSetupController : MonoBehaviour
    {
        [SerializeField]
        private ASceneSpawnPlace _SpawnPlace;
        
        public ASceneSpawnPlace SpawnPlace => _SpawnPlace;
        
        [SerializeField]
        private SceneSetupDefinition _SceneSetup;

        private void Awake()
        {
            _SceneSetup.RegisterController(this);
        }
    }
}
