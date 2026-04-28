// Author: František Holubec
// Created: 21.04.2026

using Cysharp.Threading.Tasks;

namespace EDIVE.Networking.ServerManagement
{
    public class EmptyServerRecord : ADirectServerRecord
    {
        public override UniTask<bool> PrepareForConnect()
        {
            return UniTask.FromResult(false);
        }
    }
}
