// Author: František Holubec
// Created: 15.09.2026

using System;
using System.Collections;
using EDIVE.BuildTool.PlatformConfigs;
using EDIVE.BuildTool.Signing;
using EDIVE.BuildTool.Utils;
using EDIVE.OdinExtensions.Attributes;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.ApplicationConfigs.Components
{
    [Serializable]
    public class AppSigningComponent : AApplicationConfigComponent, IStateCaptureBuildCallback, IStateRestoreBuildCallback
    {
        [EnhancedBoxGroup("Android", "@ColorTools.Green", SpaceBefore = 2)]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private AndroidSigningSettings _Android = new();

        [EnhancedBoxGroup("iOS", "@ColorTools.Cyan", SpaceBefore = 4)]
        [HideLabel]
        [InlineProperty]
        [SerializeField]
        private IosSigningSettings _Ios = new();

        public override string Label => "App Signing";
        
        public AndroidSigningSettings Android => _Android;
        public IosSigningSettings Ios => _Ios;

        public override IEnumerator Apply()
        {
            _Android.ApplyIdentity();
            _Ios.Apply();
            yield break;
        }

        public override IEnumerator LoadCurrent()
        {
            _Android.LoadCurrent();
            _Ios.LoadCurrent();
            yield break;
        }

        public override bool Validate() => _Android.Validate() && _Ios.Validate();

        public IEnumerator OnStateCapture(BuildContext context)
        {
            var data = context.GetOrCreateData<Data>();
            data._PrevUseCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            data._PrevKeystoreName = PlayerSettings.Android.keystoreName;
            data._PrevKeyAliasName = PlayerSettings.Android.keyaliasName;
            data._PrevTeamID = PlayerSettings.iOS.appleDeveloperTeamID;
            
            if (context.PlatformConfig.TryGetModule<AndroidBuildPlatformModule>(out var android))
            {
                PlayerSettings.Android.useCustomKeystore = android.UseCustomKeystore;
                if (android.UseCustomKeystore)
                    yield return ApplyAndroidSigning(context);
            }

            if (context.PlatformConfig.TryGetModule<IosBuildPlatformModule>(out _))
                _Ios.Apply();

            yield break;
        }

        public IEnumerator OnStateRestore(BuildContext context)
        {
            if (context.TryGetData<Data>(out var data))
            {
                PlayerSettings.Android.useCustomKeystore = data._PrevUseCustomKeystore;
                PlayerSettings.Android.keystoreName = data._PrevKeystoreName ?? string.Empty;
                PlayerSettings.Android.keyaliasName = data._PrevKeyAliasName ?? string.Empty;
                PlayerSettings.iOS.appleDeveloperTeamID = data._PrevTeamID ?? string.Empty;
            }
            
            // Clear stored secrets
            PlayerSettings.Android.keystorePass = string.Empty;
            PlayerSettings.Android.keyaliasPass = string.Empty;
            yield break;
        }

        private IEnumerator ApplyAndroidSigning(BuildContext context)
        {
            var resolved = _Android.TryResolve(out var data, out var error);
            if (!resolved && !Application.isBatchMode)
            {
                EditorUtility.ClearProgressBar();
                yield return AndroidKeystoreDialog.Show(error, _Android);
                resolved = _Android.TryResolve(out data, out error);
            }

            if (resolved)
                data.Apply();
            else
                context.Fail($"Android signing failed. {error}");
        }

        [Button]
        public void test()
        {
            AndroidKeystoreDialog.Show("test", _Android);
        }
        [Serializable]
        private class Data : ABuildContextData
        {
            [SerializeField]
            public bool _PrevUseCustomKeystore;

            [SerializeField]
            public string _PrevKeystoreName;

            [SerializeField]
            public string _PrevKeyAliasName;

            [SerializeField]
            public string _PrevTeamID;
        }
    }
}
