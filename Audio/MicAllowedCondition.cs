// Author: Michal Petr
// Created: 24.09.2026

using EDIVE.Conditions;
using EDIVE.Core;

namespace EDIVE.Audio
{
    public class MicAllowedCondition : ABoolCondition
    {
        protected override bool GetValue() => AppCore.Services.TryGet<AudioManager>(out var manager) && manager.AllowMic;
        
        public override void InitializeObserving()
        {
            if (!AppCore.Services.TryGet<AudioManager>(out var manager))
                return;

            manager.AllowMicChanged += OnAllowMicChanged;
        }

        public override void TerminateObserving()
        {
            if (!AppCore.Services.TryGet<AudioManager>(out var manager))
                return;

            manager.AllowMicChanged -= OnAllowMicChanged;
        }
        
        private void OnAllowMicChanged(bool value) => InvokeStateChanged();
    }
}
