// Author: František Holubec
// Created: 18.03.2026

using System;
using UnityEngine;

namespace EDIVE.BuildTool.PlatformConfigs
{
    [Serializable]
    public class PathPlatformModule : APlatformModule
    {
        public override string Label => "Path Data";
        
        [SerializeField]
        private string _PlatformName;
        public string PlatformName => _PlatformName;
        
        [SerializeField]
        private string _ConfigType;
        public string ConfigType => _ConfigType;
    }
}
