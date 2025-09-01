// Author: František Holubec
// Created: 26.08.2025

using EDIVE.Core.Services;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Environment.Sky
{
    public class SkyManager : AServiceBehaviour<SkyManager>
    {
        [ShowCreateNew]
        [SerializeField]
        private SkyDefinition _DefaultSky;
        
        private static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");

        protected override void Awake()
        {
            base.Awake();
            SetSky(_DefaultSky);
        }

        [Button]
        public void SetSky(SkyDefinition skyDefinition)
        {
            if (skyDefinition == null)
                return;
            
            var sunLight = RenderSettings.sun;
            if (sunLight != null)
            {
                sunLight.gameObject.SetActive(skyDefinition.EnableSun);
                if (skyDefinition.EnableSun)
                {
                    sunLight.transform.rotation = Quaternion.Euler(skyDefinition.SunDirection);
                    sunLight.color = skyDefinition.SunColor;
                    sunLight.intensity = skyDefinition.SunIntensity;
                    sunLight.bounceIntensity = skyDefinition.SunIndirectMultiplier;
                }
            }
            
            var skybox = RenderSettings.skybox;
            if (skybox != null)
            {
                if (skyDefinition.SkyboxTexture != null)
                    skybox.SetTexture(MAIN_TEX, skyDefinition.SkyboxTexture);
            }
            
            DynamicGI.UpdateEnvironment();
        }
    }
}
