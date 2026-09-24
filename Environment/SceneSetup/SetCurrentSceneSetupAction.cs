// Author: Michal Petr
// Created: 24.09.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Utils.Actions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.Environment.SceneSetup
{
    [Serializable]
    public class SetCurrentSceneSetupAction : IAction
    {
        [SerializeField]
        private bool _UseCurrent;
        
        [SerializeField]
        [HideIf("_UseCurrent")]
        private SceneSetupDefinition _Definition;
        
        public UniTask Execute()
        {
            if (AppCore.Services.TryGet<SceneSetupManager>(out var manager))
                return manager.SetCurrentSetupAsync(_UseCurrent ? manager.CurrentSetup : _Definition);
            return UniTask.CompletedTask;
        }
    }
}
