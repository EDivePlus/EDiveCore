using System.Collections;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace EDIVE.BuildTool.Actions
{
    [System.Serializable]
    public class SplashScreenConfigurator : ABuildAction, IPreprocessBuildAction
    {
        [SerializeField]
        [LabelWidth(160)]
        private bool _RemoveSplashScreen = true;

        public override string Tooltip => "Configures the splash screen settings for the build. Requires Unity Pro license to remove the Unity logo.";

        public SplashScreenConfigurator() { }
        public SplashScreenConfigurator(bool removeSplashScreen) { _RemoveSplashScreen = removeSplashScreen; }

        public IEnumerator OnPreprocess(BuildContext buildContext)
        {
            PlayerSettings.SplashScreen.show = !_RemoveSplashScreen;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            yield break;
        }
    }
}
