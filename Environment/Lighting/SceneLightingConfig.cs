// Author: František Holubec
// Created: 26.08.2025

using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Environment.Lighting
{
    public class SceneLightingConfig : ScriptableObject
    {
        [SerializeField]
        private Cubemap _SkyboxTexture;
        
        [PropertySpace]
        [SerializeField]
        private bool _EnableSun = true;
        
        [EnableIf(nameof(_EnableSun))]
        [SerializeField]
        private Vector3 _SunDirection = new(50, -30, 0);
        
        [EnableIf(nameof(_EnableSun))]
        [SerializeField]
        private Color _SunColor = new(1,0.92f, 0.83f);
        
        [EnableIf(nameof(_EnableSun))]
        [SerializeField]
        private float _SunIntensity = 1;
        
        [EnableIf(nameof(_EnableSun))]
        [SerializeField]
        private float _SunIndirectMultiplier = 1;

        public Cubemap SkyboxTexture => _SkyboxTexture;
        public Vector3 SunDirection => _SunDirection;
        public Color SunColor => _SunColor;
        public float SunIntensity => _SunIntensity;
        public float SunIndirectMultiplier => _SunIndirectMultiplier;
        public bool EnableSun => _EnableSun;
    }
}
