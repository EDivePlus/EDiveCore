// Author: František Holubec
// Created: 26.08.2025

using UnityEngine;

namespace EDIVE.Lighting
{
    public class SceneLightingConfig : ScriptableObject
    {
        [SerializeField]
        private Texture2D _SkyboxTexture;
        
        [SerializeField]
        private Vector3 _SunDirection = new(50, -30, 0);
        
        [SerializeField]
        private Color _SunColor = new(1,0.92f, 0.83f);
        
        [SerializeField]
        private bool _EnableSun = true;
        
        [SerializeField]
        private float _SunIntensity = 1;
        
        [SerializeField]
        private float _SunIndirectMultiplier = 1;

        public Texture2D SkyboxTexture => _SkyboxTexture;
        public Vector3 SunDirection => _SunDirection;
        public Color SunColor => _SunColor;
        public float SunIntensity => _SunIntensity;
        public float SunIndirectMultiplier => _SunIndirectMultiplier;
        public bool EnableSun => _EnableSun;
    }
}
