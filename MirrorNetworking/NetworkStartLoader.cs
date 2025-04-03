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

        public UniTask Load(Action<float> progressCallback)
        {
            var networkManager = AppCore.Services.Get<MasterNetworkManager>();
            switch (NetworkUtils.RuntimeMode)
            {
                case NetworkRuntimeMode.Server:
                    networkManager.StartServer();
                    break;
                case NetworkRuntimeMode.Client:
                    //networkManager.StartClient();
                    UniTask.Void(async () =>
                    {
                        if (string.IsNullOrWhiteSpace(_OfflineScene))
                            return;
                        await SceneManager.LoadSceneAsync(_OfflineScene, LoadSceneMode.Additive);
                        var scene = SceneManager.GetSceneByPath(_OfflineScene);
                        if (scene.IsValid())
                            SceneManager.SetActiveScene(scene);
                    });
                    break;
                case NetworkRuntimeMode.Host:
                    networkManager.StartHost();
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
            return UniTask.CompletedTask;
        }
    }
}
