// Author: František Holubec
// Created: 18.03.2026

using System;
using System.Collections;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.BuildTool.Utils;

namespace EDIVE.BuildTool.Actions
{
    [Serializable]
    public class ExtractAabAction : ABuildAction, IPostprocessBuildCallback
    {
        public override string CallbackName => "Extract AAB";
        public override string Tooltip => "Extract AAB from the generated bundle";
        
        public IEnumerator OnPostprocess(BuildContext context)
        {
            if (context.PlatformConfig.TryGetModule<AndroidBuildPlatformModule>(out var androidModule) && androidModule.BuildAndroidAppBundle)
            {
                BuildUtils.ExtractAAB(context.ResultPath.FullPath);
            }
            yield break;
        }
    }
}
