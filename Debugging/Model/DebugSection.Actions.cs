// Author: František Holubec
// Created: 15.10.2026

using System;

namespace EDIVE.Debugging
{
    // Methods with up to 4 arguments. Action runs, Func also shows the result.
    public sealed partial class DebugSection
    {
        public DebugAction Action<T1>(string label, Action<T1> action, ADebugArg<T1> arg1) =>
            Add(new DebugAction(label, false, () =>
            {
                action(arg1.Value);
                return null;
            }, arg1));

        public DebugAction Action<T1, T2>(string label, Action<T1, T2> action, ADebugArg<T1> arg1, ADebugArg<T2> arg2) =>
            Add(new DebugAction(label, false, () =>
            {
                action(arg1.Value, arg2.Value);
                return null;
            }, arg1, arg2));

        public DebugAction Action<T1, T2, T3>(string label, Action<T1, T2, T3> action, ADebugArg<T1> arg1, ADebugArg<T2> arg2, ADebugArg<T3> arg3) =>
            Add(new DebugAction(label, false, () =>
            {
                action(arg1.Value, arg2.Value, arg3.Value);
                return null;
            }, arg1, arg2, arg3));

        public DebugAction Action<T1, T2, T3, T4>(string label, Action<T1, T2, T3, T4> action, ADebugArg<T1> arg1, ADebugArg<T2> arg2, ADebugArg<T3> arg3, ADebugArg<T4> arg4) =>
            Add(new DebugAction(label, false, () =>
            {
                action(arg1.Value, arg2.Value, arg3.Value, arg4.Value);
                return null;
            }, arg1, arg2, arg3, arg4));

        public DebugAction Func<TResult>(string label, Func<TResult> func) =>
            Add(new DebugAction(label, true, () => func()));

        public DebugAction Func<T1, TResult>(string label, Func<T1, TResult> func, ADebugArg<T1> arg1) =>
            Add(new DebugAction(label, true, () => func(arg1.Value), arg1));

        public DebugAction Func<T1, T2, TResult>(string label, Func<T1, T2, TResult> func, ADebugArg<T1> arg1, ADebugArg<T2> arg2) =>
            Add(new DebugAction(label, true, () => func(arg1.Value, arg2.Value), arg1, arg2));

        public DebugAction Func<T1, T2, T3, TResult>(string label, Func<T1, T2, T3, TResult> func, ADebugArg<T1> arg1, ADebugArg<T2> arg2, ADebugArg<T3> arg3) =>
            Add(new DebugAction(label, true, () => func(arg1.Value, arg2.Value, arg3.Value), arg1, arg2, arg3));

        public DebugAction Func<T1, T2, T3, T4, TResult>(string label, Func<T1, T2, T3, T4, TResult> func, ADebugArg<T1> arg1, ADebugArg<T2> arg2, ADebugArg<T3> arg3, ADebugArg<T4> arg4) =>
            Add(new DebugAction(label, true, () => func(arg1.Value, arg2.Value, arg3.Value, arg4.Value), arg1, arg2, arg3, arg4));
    }
}
