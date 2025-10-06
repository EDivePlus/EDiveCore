using System;
using System.Collections.Generic;
using EDIVE.BuildTool.PathResolving;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.BuildTool.Presets;
using EDIVE.BuildTool.Utils;
using EDIVE.Core.Versions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EDIVE.BuildTool
{
    [Serializable]
    public class BuildContext
    {
        [SerializeField]
        private AppVersionDefinition _VersionDefinition;

        [SerializeField]
        private AppVersion _Version;
        
        [SerializeField]
        private FilePath _ResultPath;

        [SerializeField]
        private BuildStateType _State;

        [SerializeField]
        private BuildOptions _Options;

        [SerializeField]
        private BuildResult _Result;

        [SerializeField]
        private BuildReport _Report;

        [SerializeField]
        private List<string> _Defines = new();

        [SerializeField]
        private BuildUserConfig _UserConfig;
        
        [SerializeField]
        private ABuildPlatformConfig _PlatformConfig;
        
        [SerializeField]
        private List<string> _Scenes;
        
        public AppVersionDefinition VersionDefinition { get => _VersionDefinition; set => _VersionDefinition = value; }
        public BuildStateType State { get => _State; set => _State = value; }
        public BuildOptions Options { get => _Options; set => _Options = value; }
        public BuildResult Result { get => _Result; set => _Result = value; }
        public BuildReport Report { get => _Report; set => _Report = value; }
        public List<string> Defines { get => _Defines; set => _Defines = value; }
        public FilePath ResultPath { get => _ResultPath; set => _ResultPath = value; }
        public BuildUserConfig UserConfig { get => _UserConfig; set => _UserConfig = value; }
        public ABuildPlatformConfig PlatformConfig { get => _PlatformConfig; set => _PlatformConfig = value; }
        public List<string> Scenes { get => _Scenes; set => _Scenes = value; }

        public BuildContext(BuildOptions options = BuildOptions.None)
        {
            _Options = options;
            _State = BuildStateType.NotStarted;
        }
    }
}
