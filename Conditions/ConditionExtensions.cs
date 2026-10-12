// Author: František Holubec
// Created: 12.10.2026

using System;
using EDIVE.NativeUtils;

namespace EDIVE.Conditions
{
    public static class ConditionExtensions
    {
        // Listens to StateChanged and starts observing, disposing the result undoes both
        public static IDisposable ObserveDisposable(this ICondition condition, Action onStateChanged)
        {
            if (condition == null || onStateChanged == null)
                return DisposableUtils.Empty;

            // Before observing, it can report its first state right away
            condition.StateChanged += onStateChanged;
            condition.InitializeObserving();
            return DisposableUtils.Create(() =>
            {
                condition.StateChanged -= onStateChanged;
                condition.TerminateObserving();
            });
        }
    }
}
