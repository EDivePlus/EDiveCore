// Author: Michal Petr
// Created: 16.06.2026

using Cysharp.Threading.Tasks;

namespace EDIVE.ServiceHub.SaveData.SyncHandlers
{
    public class ImmediateSaveDataSyncHandler : ASaveDataSyncHandler
    {
        protected override SaveDataDirtyFlag HandledFlags => SaveDataDirtyFlag.Immediate;

        protected override void ScheduleSync(string key, string json)
        {
            // No auth yet, send once it is back
            if (!Context.Auth.IsValid())
            {
                QueueRetry(key, json);
                return;
            }
            var ct = _cts.Token;
            UniTask.Void(async () =>
            {
                var result = await PutSaveDataAsync(Context, key, json, ct);
                if (result.IsSuccess && result.Result is { Status: 0 })
                {
                    RaiseSyncSuccess(key, result.Result.Data?.UpdatedAt);
                    return;
                }
                RaiseSyncFailure(key, result.ErrorMessage ?? result.Result?.Message);
                if (IsRetryable(result.StatusCode))
                    QueueRetry(key, json);
            });
        }
    }
}
