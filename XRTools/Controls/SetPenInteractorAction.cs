// Author: František Holubec
// Created: 02.10.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Utils.Actions;
using UnityEngine;

namespace EDIVE.XRTools.Controls
{
    [Serializable]
    public class SetPenInteractorAction : IAction
    {
        [SerializeField]
        private bool _Enabled;

        public UniTask Execute()
        {
            if (AppCore.Services.TryGet<PenInteractorManager>(out var manager))
                manager.PenInteractorEnabled = _Enabled;
            return UniTask.CompletedTask;
        }
    }
}
