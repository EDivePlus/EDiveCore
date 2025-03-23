// Author: František Holubec
// Created: 22.03.2025

using System;
using Cysharp.Threading.Tasks;
using EDIVE.OdinExtensions.Attributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EDIVE.AppLoading.Finalizers
{
    [Serializable]
    public class LoadSceneLoadFinalizer : ILoadFinalizer
    {
        [SerializeField]
        [SceneReference(SceneReferenceType.Path, true)]
        private string _Scene;

        public UniTask<bool> TryFinalizeLoad()
        {
            SceneManager.LoadScene(_Scene);
            return UniTask.FromResult(true);
        }
    }
}
