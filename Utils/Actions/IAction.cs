// Author: Michal Petr
// Created: 22.09.2026

using Cysharp.Threading.Tasks;
using EDIVE.OdinExtensions.Attributes;

namespace EDIVE.Utils.Actions
{
    [EnhancedTypeSelector(true, 1)]
    public interface IAction
    {
        UniTask Execute();
    }
}
