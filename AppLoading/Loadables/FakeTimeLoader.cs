// Author: František Holubec
// Created: 27.03.2025

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EDIVE.AppLoading.Loadables
{
    [Serializable]
    public class FakeTimeLoader : ILoadable
    {
        [SerializeField]
        private float _Time;

        public async UniTask Load(Action<float> progressCallback)
        {
            await UniTask.WaitForSeconds(_Time);
        }
    }
}
