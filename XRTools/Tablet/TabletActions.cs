// Author: František Holubec
// Created: 29.10.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Utils.Actions;

namespace EDIVE.XRTools.Tablet
{
    // Opens the tablet in front of the player
    [Serializable]
    public class OpenTabletAction : IAction
    {
        public UniTask Execute()
        {
            if (AppCore.Services.TryGet<TabletController>(out var tablet) && !(tablet.IsOpen && tablet.IsInView))
                tablet.RepositionTablet();
            return UniTask.CompletedTask;
        }
    }

    [Serializable]
    public class CloseTabletAction : IAction
    {
        public UniTask Execute()
        {
            if (AppCore.Services.TryGet<TabletController>(out var tablet))
                tablet.SetOpen(false);
            return UniTask.CompletedTask;
        }
    }
}
