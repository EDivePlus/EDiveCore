// Author: Michal Petr
// Created: 24.09.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Utils.Actions;
using UnityEngine;

namespace EDIVE.Audio
{
    [Serializable]
    public class SetAllowMicAction : IAction
    {
        [SerializeField]
        private bool _AllowMic;
        
        public UniTask Execute()
        {
            if (AppCore.Services.TryGet<AudioManager>(out var manager))
                manager.AllowMic = _AllowMic;
            return UniTask.CompletedTask;
        }
    }
}
