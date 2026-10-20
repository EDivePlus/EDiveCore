// Author: Michal Petr
// Created: 20.10.2026

using System;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.Utils.Actions;
using EDIVE.XRTools.ActionWheel;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EDIVE.XRTools.LaserPointer
{
    [Serializable]
    public class SetLaserPointerActiveAction : IContextAction<ActionWheelContext>
    {
        [SerializeField]
        [Tooltip("Pointer goes on the hand that opened the action wheel.")]
        private bool _Active = true;

        public UniTask Execute() => Execute(default);

        public UniTask Execute(ActionWheelContext context)
        {
            if (AppCore.Services.TryGet<LaserPointerController>(out var controller))
                controller.SetActiveHand(_Active ? context.Hand : InteractorHandedness.None);
            return UniTask.CompletedTask;
        }
    }
}
