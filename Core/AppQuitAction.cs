// Author: František Holubec
// Created: 09.10.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.Utils.Actions;
using UnityEngine;

namespace EDIVE.Core
{
    [Serializable]
    public class AppQuitAction : IAction
    {
        public UniTask Execute()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            return UniTask.CompletedTask;
        }
    }
}
