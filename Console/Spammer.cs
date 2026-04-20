// Author: František Holubec
// Created: 20.04.2026

using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DefaultNamespace
{
    public class Spammer : MonoBehaviour
    {
        private void Awake()
        {
            LogAll().Forget();
            LogWarnings().Forget();
        }

        private static async UniTaskVoid LogAll()
        {
            while (true)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(4));
                Debug.Log("Spammer is spamming...");
            }
        }
        
        private static async UniTaskVoid LogWarnings()
        {
            while (true)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(10));
                Debug.LogError("Spamming error...");
                Debug.LogWarning("Spamming warning...");
            }
        }
    }
}
