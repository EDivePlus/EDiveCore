// Author: Michal Petr
// Created: 16.06.2026

using System;
using Cysharp.Threading.Tasks;

namespace EDIVE.ServiceHub.SaveData.SyncHandlers
{
    public class ImmediateSaveDataSyncHandler : ASaveDataSyncHandler
    {
        protected override SaveDataDirtyFlag HandledFlags => SaveDataDirtyFlag.Immediate;

        public override event Action<(string Key, DateTime? UpdatedAt)> SyncSuccess;
        public override event Action<(string Key, string Error)> SyncFailure;

        protected override void ScheduleSync(string key, string json)
        {
            if (!Context.Auth.IsValid())
                return;

            UniTask.Void(async () =>
            {
                var result = await PutSaveDataAsync(Context, key, json, _cts.Token);
                if (result.IsSuccess)
                    SyncSuccess?.Invoke((key, Normalize(result.Result?.Data?.UpdatedAt)));
                else
                    SyncFailure?.Invoke((key, result.ErrorMessage));
            });
        }
    }
}
