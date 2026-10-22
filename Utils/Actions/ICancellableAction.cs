// Author: Michal Petr
// Created: 22.10.2026

using System.Threading;
using Cysharp.Threading.Tasks;

namespace EDIVE.Utils.Actions
{
    // Runs until the token is cancelled, e.g. while a button is held
    public interface ICancellableAction : IAction
    {
        UniTask Execute(CancellationToken cancellationToken);
    }
}
