// Author: Michal Petr
// Created: 20.10.2026

using Cysharp.Threading.Tasks;

namespace EDIVE.Utils.Actions
{
    public interface IContextAction<in TContext> : IAction
    {
        UniTask Execute(TContext context);
    }
}
