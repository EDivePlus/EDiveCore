// Author: Michal Petr
// Created: 22.10.2026

using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Core;
using EDIVE.XRTools.Controls;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace EDIVE.XRTools.ScreenInteraction
{
    // Holds what the screen controls grab at its attach transform
    public class ScreenInteractor : XRBaseInputInteractor
    {
        // XRI applies a queued activate a frame later, a shorter press would get lost
        private const int MIN_ACTIVATE_FRAMES = 2;

        protected override void Awake()
        {
            if (AppCore.Services.TryGet<InteractionManagerProvider>(out var provider))
                interactionManager = provider.InteractionManager;

            base.Awake();
            activateInput.inputSourceMode = XRInputButtonReader.InputSourceMode.ManualValue;
        }

        // Activates the held interactable until cancelled
        public async UniTask Activate(CancellationToken cancellationToken)
        {
            activateInput.QueueManualState(true, 1f);
            await UniTask.WhenAll(UniTask.WaitUntilCanceled(cancellationToken), UniTask.DelayFrame(MIN_ACTIVATE_FRAMES));
            if (this != null)
                activateInput.QueueManualState(false, 0f);
        }
    }
}
