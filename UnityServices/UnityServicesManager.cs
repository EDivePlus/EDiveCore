// Author: Michal Petr
// Created: 14.05.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using Unity.Services.Authentication;
using UnityEngine;

namespace EDIVE.UnityServices
{
    public class UnityServicesManager : MonoBehaviour, ILoadable
    {
        public async UniTask Load(Action<float> progressCallback)
        {
            await Unity.Services.Core.UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
}
