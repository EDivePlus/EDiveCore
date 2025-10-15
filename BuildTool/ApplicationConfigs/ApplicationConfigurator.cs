// Author: František Holubec
// Created: 15.10.2025

using System;
using System.Collections;
using EDIVE.BuildTool.Actions;
using UnityEngine;

namespace EDIVE.BuildTool.ApplicationConfigs
{
    [Serializable]
    public class ApplicationConfigurator : ABuildAction, IPreprocessBuildAction
    {
        [SerializeField]
        private ApplicationConfig _Config;

        public ApplicationConfigurator(ApplicationConfig config)
        {
            _Config = config;
        }
        
        public IEnumerator OnPreprocess(BuildContext buildContext)
        {
            yield return _Config.Apply();
        }
    }
}
