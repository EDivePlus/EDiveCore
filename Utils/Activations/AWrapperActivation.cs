// Author: František Holubec
// Created: 04.09.2025

using System;

namespace EDIVE.Utils.Activations
{
    [Serializable]
    public abstract class AWrapperActivation : IActivation
    {
        private event Action InnerEvent;

        public void RegisterActivationListener(Action onActivate)
        {
            var wasEmpty = InnerEvent == null;
            InnerEvent += onActivate;
            if (wasEmpty)
                StartListening();
        }

        public void UnregisterActivationListener(Action onActivate)
        {
            InnerEvent -= onActivate;
            if (InnerEvent == null)
                StopListening();
        }
        
        protected void InvokeListeners() => InnerEvent?.Invoke();
        
        protected abstract void StartListening();
        protected abstract void StopListening();
    }
}
