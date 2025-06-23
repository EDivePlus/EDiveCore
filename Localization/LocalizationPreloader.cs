using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.DataStructures;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace EDIVE.Localization
{
    [System.Serializable]
    public class UnityLocalizationPreloader : ILoadable
    {
        [SerializeField]
        private PlatformSpecificValue<PreloadBehavior> _PreloadBehavior = new()
        {
            { PreloadBehavior.PreloadSelectedLocale, RuntimePlatform.WindowsPlayer, RuntimePlatform.Android },
            { PreloadBehavior.PreloadSelectedLocaleAndFallbacks, RuntimePlatform.WebGLPlayer }
        };

        public async UniTask Load(System.Action<float> progressCallback)
        {
            LocalizationSettings.PreloadBehavior = _PreloadBehavior.GetCurrentPlatformValue();
            LocalizationSettings.InitializeSynchronously = false;

            var initializationOperation = LocalizationSettings.InitializationOperation;
            while (!initializationOperation.IsDone)
            {
                await UniTask.Yield();
                progressCallback?.Invoke(initializationOperation.PercentComplete);
            }

            if (initializationOperation.Status == AsyncOperationStatus.Failed)
            {
                Debug.LogError("Localization initialization failed! See exception below.");
                Debug.LogException(initializationOperation.OperationException);
            }
        }
    }
}

