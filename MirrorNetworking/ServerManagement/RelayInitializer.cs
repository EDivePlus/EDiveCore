// Author: František Holubec
// Created: 15.06.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.AppLoading.Loadables;
using LightReflectiveMirror;
using UnityEngine;

namespace EDIVE.MirrorNetworking.ServerManagement
{
    [RequireComponent(typeof(LightReflectiveMirrorTransport))]
    public class RelayInitializer : MonoBehaviour, ILoadable
    {
        [SerializeField]
        private RelayConfig _Config;

        public UniTask Load(Action<float> progressCallback)
        {
            var transport = GetComponent<LightReflectiveMirrorTransport>();
            transport.ConnectToRelay();
            return UniTask.CompletedTask;
        }
    }
}
