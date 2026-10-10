// Author: František Holubec
// Created: 09.10.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.Utils.Actions;

namespace EDIVE.Core.Restart
{
    [Serializable]
    public class AppRestartAction : IAction
    {
        public UniTask Execute() => AppRestartUtility.RestartAsync();
    }
}
