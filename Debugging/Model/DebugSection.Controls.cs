// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.ScriptableArchitecture.Variables.Impl;
using EDIVE.Utils.Actions;

#if R3
using R3;
#endif

namespace EDIVE.Debugging
{
    // Control factories. Getter only means read only.
    public sealed partial class DebugSection
    {
        // Buttons

        public DebugButton Button(string label, Action action) => Add(new DebugButton(label, action));

        public DebugButton Button(string label, Func<UniTask> action) => Add(new DebugButton(label, () => action().Forget()));

        public DebugButton Button(string label, IAction action) => Add(new DebugButton(label, () => action.Execute().Forget()));

        public DebugButtonRow Buttons(params (string label, Action action)[] buttons) => Add(new DebugButtonRow(buttons));

        // Bool

        public DebugToggle Toggle(string label, Func<bool> getter, Action<bool> setter = null) => Add(new DebugToggle(label, getter, setter));

        public DebugToggle Toggle(AScriptableVariable<bool> variable, string label = null) => Add(Bind(new DebugToggle(label ?? variable.name, variable.GetValue, Setter(variable)), variable));

        // Int

        public DebugIntField Int(string label, Func<int> getter, Action<int> setter = null) => Add(new DebugIntField(label, getter, setter));

        public DebugIntField Int(AScriptableVariable<int> variable, string label = null) => Add(Bind(new DebugIntField(label ?? variable.name, variable.GetValue, Setter(variable)), variable));

        // Float

        public DebugFloatField Float(string label, Func<float> getter, Action<float> setter = null) => Add(new DebugFloatField(label, getter, setter));

        public DebugFloatField Float(AScriptableVariable<float> variable, string label = null) => Add(Bind(new DebugFloatField(label ?? variable.name, variable.GetValue, Setter(variable)), variable));

        // String

        public DebugTextField String(string label, Func<string> getter, Action<string> setter = null) => Add(new DebugTextField(label, getter, setter));

        public DebugTextField String(AScriptableVariable<string> variable, string label = null) => Add(Bind(new DebugTextField(label ?? variable.name, variable.GetValue, Setter(variable)), variable));

        // Choices

        public DebugDropdown Enum<T>(string label, Func<T> getter, Action<T> setter = null) where T : struct, Enum
        {
            var values = (T[]) System.Enum.GetValues(typeof(T));
            var names = System.Enum.GetNames(typeof(T));
            return Add(new DebugDropdown(label,
                () => Array.IndexOf(values, getter()),
                setter == null ? null : i => setter(values[i]),
                () => names));
        }

        public DebugDropdown Dropdown(string label, Func<int> getter, Action<int> setter, IReadOnlyList<string> options) =>
            Add(new DebugDropdown(label, getter, setter, () => options));

        public DebugDropdown Dropdown(string label, Func<string> getter, Action<string> setter, IReadOnlyList<string> options)
        {
            return Add(new DebugDropdown(label,
                () => IndexOf(options, getter()),
                setter == null ? null : i => setter(options[i]),
                () => options));
        }

        // Read only

        public DebugWatch Watch(string label, Func<object> getter) => Add(new DebugWatch(label, getter));

        public DebugWatch Watch<T>(AScriptableVariable<T> variable, string label = null) =>
            Add(new DebugWatch(label ?? variable.name, () => variable.GetValue()).RefreshOn(a => variable.ValueChanged += a, a => variable.ValueChanged -= a));

#if R3
        public DebugToggle Toggle(string label, ReactiveProperty<bool> property) => Add(new DebugToggle(label, () => property.Value, v => property.Value = v).RefreshOn(property));

        public DebugIntField Int(string label, ReactiveProperty<int> property) => Add(new DebugIntField(label, () => property.Value, v => property.Value = v).RefreshOn(property));

        public DebugFloatField Float(string label, ReactiveProperty<float> property) => Add(new DebugFloatField(label, () => property.Value, v => property.Value = v).RefreshOn(property));

        public DebugTextField String(string label, ReactiveProperty<string> property) => Add(new DebugTextField(label, () => property.Value, v => property.Value = v).RefreshOn(property));

        public DebugDropdown Enum<T>(string label, ReactiveProperty<T> property) where T : struct, Enum => Enum(label, () => property.Value, v => property.Value = v).RefreshOn(property);

        public DebugWatch Watch<T>(string label, ReadOnlyReactiveProperty<T> property) => Add(new DebugWatch(label, () => property.CurrentValue).RefreshOn(property));
#endif

        private static Action<T> Setter<T>(AScriptableVariable<T> variable) => variable.IsReadOnly ? null : variable.SetValue;

        private static TControl Bind<TControl>(TControl control, AScriptableVariable variable) where TControl : ADebugControl<TControl> =>
            control.RefreshOn(a => variable.ValueChanged += a, a => variable.ValueChanged -= a);

        private static int IndexOf(IReadOnlyList<string> options, string value)
        {
            for (var i = 0; i < options.Count; i++)
            {
                if (options[i] == value)
                    return i;
            }
            return -1;
        }
    }
}
