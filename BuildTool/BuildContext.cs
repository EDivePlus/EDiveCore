using System;
using System.Collections.Generic;
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
        private BuildStateType _State;

        [SerializeField]
        private BuildOptions _Options;

        [SerializeField]
        private BuildResult _Result;

        [SerializeField]
        private BuildReport _Report;

        [SerializeField]
        private List<string> _Defines = new();

        public AppVersionDefinition VersionDefinition { get => _VersionDefinition; set => _VersionDefinition = value; }
        public BuildStateType State { get => _State; set => _State = value; }
        public BuildOptions Options { get => _Options; set => _Options = value; }
        public BuildResult Result { get => _Result; set => _Result = value; }
        public BuildReport Report { get => _Report; set => _Report = value; }
        public List<string> Defines { get => _Defines; set => _Defines = value; }

        public BuildContext(BuildOptions options = BuildOptions.None)
        {
            _Options = options;
            _State = BuildStateType.NotStarted;
        }
    }
}
