// Author: František Holubec
// Created: 05.09.2025

using System.Collections.Generic;
using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    public class SceneSetupController : MonoBehaviour
    {
        [SerializeField]
        private ASceneSpawnPlace _SpawnPlace;
        
        [SerializeField]
        private List<Light> _Lights;

        public ASceneSpawnPlace SpawnPlace => _SpawnPlace;
        public List<Light> Lights => _Lights;
        
        [SerializeField]
        private SceneSetupDefinition _SceneSetup;

        private void Awake()
        {
            _SceneSetup.RegisterController(this);
        }
        
        public void SetLightsActive(bool active)
        {
            foreach (var sceneLight in _Lights)
            {
                if (sceneLight != null)
                    sceneLight.gameObject.SetActive(active);
            }
        }
    }
}
