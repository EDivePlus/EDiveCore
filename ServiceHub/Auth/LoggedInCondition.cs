// Author: Michal Petr
// Created: 04.05.2026

using System;
using EDIVE.Conditions;

namespace EDIVE.ServiceHub.Auth
{
    [Serializable]
    public class LoggedInCondition : ABoolCondition
    {
        protected override bool GetValue() => AuthStorage.Client.HasSession;

        public override void InitializeObserving() => AuthStorage.Client.SessionChanged += InvokeStateChanged;
        public override void TerminateObserving() => AuthStorage.Client.SessionChanged -= InvokeStateChanged;
    }
}
