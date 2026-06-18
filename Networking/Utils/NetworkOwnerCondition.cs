// Author: František Holubec
// Created: 18.06.2026

using System;
using EDIVE.Conditions;
using UnityEngine;

namespace EDIVE.Networking.Utils
{
    [Serializable]
    public class NetworkOwnerCondition : ABoolCondition
    {
        [SerializeField]
        private NetworkOwnerObserver _Observer;

        protected override bool GetValue()
        {
            return _Observer != null && _Observer.isOwner;
        }

        public override void InitializeObserving()
        {
            base.InitializeObserving();
            if (_Observer != null)
                _Observer.OwnershipChanged += OnOwnershipChanged;
        }

        public override void TerminateObserving()
        {
            base.TerminateObserving();
            if (_Observer != null)
                _Observer.OwnershipChanged -= OnOwnershipChanged;
        }

        private void OnOwnershipChanged() => InvokeStateChanged();
    }
}
