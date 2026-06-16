// Author: Michal Petr
// Created: 16.06.2026

using System.Threading;
using Cysharp.Threading.Tasks;

namespace EDIVE.ServiceHub.SaveData.SyncHandlers
{
    public class BatchSaveDataSyncHandler : APeriodicSaveDataSyncHandler
    {
        protected override SaveDataDirtyFlag HandledFlags => SaveDataDirtyFlag.OnBatch;
        
        protected override async UniTask WaitForSync(CancellationToken ct)
        {
            await UniTask.Delay(Service.Settings.BatchSaveDataSyncInterval, cancellationToken: ct);
        }
    }
}
