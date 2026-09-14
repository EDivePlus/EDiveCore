// Author: František Holubec
// Created: 20.07.2026

using System;
using System.Collections;
using EDIVE.OdinExtensions.Attributes;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EDIVE.BuildTool.PlatformConfigs
{
    [Serializable]
    public class IosBuildPlatformModule : ABuildTargetPlatformModule
    {
        public override string PlatformName => "iOS";
        public override NamedBuildTarget NamedBuildTarget => NamedBuildTarget.iOS;
        public override BuildTarget BuildTarget => BuildTarget.iOS;
        public override string BuildExtension => "";
        
        [EnhancedBoxGroup("Signing", "@ColorTools.Green", SpaceBefore = 4)]
        [Tooltip("The developer team comes from the application config.")]
        [SerializeField]
        internal bool _AutoSign;

        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private bool _SymlinkLibraries = true;
        
        [EnhancedBoxGroup("Build")]
        [SerializeField]
        private XcodeBuildConfig _XCodeConfig;
        
        public override IEnumerator OnStateCapture(BuildContext context)
        {
            yield return base.OnStateCapture(context);
            var data = context.GetOrCreateData<Data>();
            
            data._PrevXCodeConfig = EditorUserBuildSettings.iOSXcodeBuildConfig;
            EditorUserBuildSettings.iOSXcodeBuildConfig = _XCodeConfig;

            if (_SymlinkLibraries) context.Options |= BuildOptions.SymlinkSources;
            data._PrevSymlinkLibraries = EditorUserBuildSettings.symlinkSources;
            EditorUserBuildSettings.symlinkSources = _SymlinkLibraries;
            
            data._PrevAutoSign = PlayerSettings.iOS.appleEnableAutomaticSigning;
            PlayerSettings.iOS.appleEnableAutomaticSigning = _AutoSign;
        }

        public override IEnumerator OnStateRestore(BuildContext context)
        {
            yield return base.OnStateRestore(context);
            if (!context.TryGetData<Data>(out var data))
                yield break;
            
            EditorUserBuildSettings.iOSXcodeBuildConfig = data._PrevXCodeConfig;
            EditorUserBuildSettings.symlinkSources = data._PrevSymlinkLibraries;

            PlayerSettings.iOS.appleEnableAutomaticSigning = data._PrevAutoSign;
        }
        
        [Serializable]
        private class Data : ABuildContextData
        {
            [SerializeField]
            public XcodeBuildConfig _PrevXCodeConfig;

            [SerializeField]
            public bool _PrevSymlinkLibraries;
            
            [SerializeField]
            public bool _PrevAutoSign;
        }
    }
}
