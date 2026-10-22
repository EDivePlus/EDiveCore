// Author: Michal Petr
// Created: 20.10.2026

using System.Threading;
using Cysharp.Threading.Tasks;

namespace EDIVE.Utils.Actions
{
    public static class ActionExtensions
    {
        /// <summary>
        /// Executes with the context when the action accepts it, otherwise plain.
        /// </summary>
        public static UniTask Execute<TContext>(this IAction action, TContext context)
        {
            return action is IContextAction<TContext> contextAction ? contextAction.Execute(context) : action.Execute();
        }

        /// <summary>
        /// Executes until cancelled when the action supports it, otherwise plain.
        /// </summary>
        public static UniTask Execute(this IAction action, CancellationToken cancellationToken)
        {
            return action is ICancellableAction cancellableAction ? cancellableAction.Execute(cancellationToken) : action.Execute();
        }
    }
}
