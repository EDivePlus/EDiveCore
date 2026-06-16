// Author: Michal Petr
// Created: 16.06.2026

using System.Threading;
using Cysharp.Threading.Tasks;

namespace EDIVE.ServiceHub.SaveData.SyncHandlers
{
    public class EndOfFrameSaveDataSyncHandler : APeriodicSaveDataSyncHandler
    {
        protected override SaveDataDirtyFlag HandledFlags => SaveDataDirtyFlag.OnEndOfFrame;
        
        protected override async UniTask WaitForSync(CancellationToken ct)
        {
            await UniTask.WaitForEndOfFrame(cancellationToken: ct);
        }
    }
}
