// Author: František Holubec
// Created: 28.10.2026

using System;
using EDIVE.Conditions;

namespace EDIVE.ServiceHub.SaveData
{
    // Some save data was refused by the server and is only kept on this device
    [Serializable]
    public class SaveDataUnsavedCondition : ABoolCondition
    {
        protected override bool GetValue() => SaveDataRejections.Any;

        public override void InitializeObserving() => SaveDataRejections.Changed += InvokeStateChanged;
        public override void TerminateObserving() => SaveDataRejections.Changed -= InvokeStateChanged;
    }
}
