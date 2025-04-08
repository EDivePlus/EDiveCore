// Author: František Holubec
// Created: 22.03.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using EDIVE.Core;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.MirrorNetworking
{
    [Serializable]
    public class NetworkStartLoader : ILoadable
    {
        [SerializeField]
        [SceneReference(SceneReferenceType.Path, true)]
        private string _OfflineScene;

        [SerializeField]
        [SceneReference(SceneReferenceType.Path, true)]
        private string _OnlineScene;

        public UniTask Load(Action<float> progressCallback)
        {
            var networkManager = AppCore.Services.Get<MasterNetworkManager>();
            switch (NetworkUtils.RuntimeMode)
            {
                case NetworkRuntimeMode.Server:
                    networkManager.StartServer();
                    LoadScene(_OnlineScene).Forget();
                    break;
                case NetworkRuntimeMode.Client:
                    //networkManager.StartClient();
                    LoadScene(_OfflineScene).Forget();
                    break;
                case NetworkRuntimeMode.Host:
                    networkManager.StartHost();
                    LoadScene(_OnlineScene).Forget();
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
            return UniTask.CompletedTask;
        }

        private async UniTaskVoid LoadScene(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
                return;
            await SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath(scenePath);
            if (scene.IsValid())
                SceneManager.SetActiveScene(scene);
        }
    }
}
