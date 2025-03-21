using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace EDIVE.BuildTool.Actions
{
    [System.Serializable]
    public class SplashScreenConfigurator : ABuildAction
    {
        [SerializeField]
        [LabelWidth(160)]
        private bool _RemoveSplashScreen = true;

        public bool RemoveSplashScreen { get => _RemoveSplashScreen; set => _RemoveSplashScreen = value; }

        public SplashScreenConfigurator() { }
        public SplashScreenConfigurator(bool removeSplashScreen) { _RemoveSplashScreen = removeSplashScreen; }

        public override UniTask OnPreBuildAfterDefines(BuildContext buildContext)
        {
            if (Application.HasProLicense())
            {
                PlayerSettings.SplashScreen.show = !_RemoveSplashScreen;
                PlayerSettings.SplashScreen.showUnityLogo = false;
            }
            return UniTask.CompletedTask;
        }
    }
}
