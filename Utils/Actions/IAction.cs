// Author: Michal Petr
// Created: 22.09.2026

using Cysharp.Threading.Tasks;

namespace EDIVE.Utils.Actions
{
    public interface IAction
    {
        UniTask Execute();
    }
}
