// Author: František Holubec
// Created: 26.08.2025

using EDIVE.Core.Services;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Lighting
{
    public class LightingManager : AServiceBehaviour<LightingManager>
    {
        [ShowCreateNew]
        [SerializeField]
        private SceneLightingConfig _DefaultLightingConfig;
        
        private static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");

        protected override void Awake()
        {
            base.Awake();
            SetLighting(_DefaultLightingConfig);
        }

        [Button]
        public void SetLighting(SceneLightingConfig lightingConfig)
        {
            if (lightingConfig == null)
                return;
            
            var sunLight = RenderSettings.sun;
            if (sunLight != null)
            {
                sunLight.enabled = lightingConfig.EnableSun;
                sunLight.transform.rotation = Quaternion.Euler(lightingConfig.SunDirection);
                sunLight.color = lightingConfig.SunColor;
                sunLight.intensity = lightingConfig.SunIntensity;
                sunLight.bounceIntensity = lightingConfig.SunIndirectMultiplier;
                DynamicGI.UpdateEnvironment();
            }
            
            var skybox = RenderSettings.skybox;
            if (skybox != null)
            {
                if (lightingConfig.SkyboxTexture != null)
                    skybox.SetTexture(MAIN_TEX, lightingConfig.SkyboxTexture);
            }
        }
    }
}
