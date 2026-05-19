// Author: Michal Petr
// Created: 20.05.2026

using Cysharp.Threading.Tasks;

namespace EDIVE.Networking.ServerManagement
{
    public class DirectJoinServerListAdapter : AServerListAdapter
    {
        public override UniTask<(bool, ServerRecord)> TryHandleJoinRequest(IJoinRequest request)
        {
            if (request is not DirectJoinRequest directJoinRequest)
                return UniTask.FromResult((false, default(ServerRecord)));
            
            var endpoint = new AddressServerEndpoint
            {
                Name = $"Direct Join {directJoinRequest.Address}:{directJoinRequest.Port}",
                Address = directJoinRequest.Address,
                Port = directJoinRequest.Port
            };
            
            return UniTask.FromResult((true, ServerRecord.CreateUnknown(endpoint)));
        }
    }
}
