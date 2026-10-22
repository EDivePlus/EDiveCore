// Author: Michal Petr
// Created: 22.10.2026

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using EDIVE.Conditions;
using EDIVE.NativeUtils;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.Utils.Actions;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EDIVE.XRTools.ScreenInteraction
{
    // Wired in the inspector from conditions and actions
    [Serializable]
    public class CustomScreenInteractionCommand : AScreenInteractionCommand
    {
        [SerializeField]
        private ScreenInteractionCommandSlot _ShownWhen = ScreenInteractionCommandSlot.Held;

        [Tooltip("Runs while pressed, cancellable actions stop on release")]
        [SerializeField]
        private bool _Hold;

        [EnhancedBoxGroup("Conditions", SpaceBefore = 4)]
        [LabelText("Visible")]
        [SerializeReference]
        private ICondition _VisibleCondition;

        [EnhancedBoxGroup("Conditions")]
        [LabelText("Unlocked")]
        [SerializeReference]
        private ICondition _UnlockedCondition;

        [PropertySpace]
        [ListDrawerSettings(ShowFoldout = false)]
        [SerializeReference]
        private List<IAction> _Actions = new();

        public override ScreenInteractionCommandSlot ShownWhen => _ShownWhen;
        public override bool IsHold => _Hold;
        public override bool IsVisible => _VisibleCondition?.Evaluate() ?? true;
        public override bool IsUnlocked => _UnlockedCondition?.Evaluate() ?? true;

        public override UniTask Execute(ScreenInteractionCommandContext context, CancellationToken cancellationToken)
        {
            return UniTask.WhenAll(_Actions.Select(action => action.Execute(cancellationToken)));
        }

        public override IDisposable Observe(Action onChanged)
        {
            var visible = _VisibleCondition.ObserveDisposable(onChanged);
            var unlocked = _UnlockedCondition.ObserveDisposable(onChanged);
            return DisposableUtils.Create(() =>
            {
                visible.Dispose();
                unlocked.Dispose();
            });
        }
    }
}
