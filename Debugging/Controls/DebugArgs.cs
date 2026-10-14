// Author: František Holubec
// Created: 15.10.2026

using System;
using System.Collections.Generic;
using System.Linq;

namespace EDIVE.Debugging
{
    // Argument factories for actions: s.Action("Spawn", (string p, int n) => ..., Arg.Options("Prefab", names), Arg.Int("Count", 3)).
    public static class Arg
    {
        public static DebugIntArg Int(string label, int value = 0) => new(label, value);

        public static DebugFloatArg Float(string label, float value = 0f) => new(label, value);

        public static DebugStringArg String(string label, string value = "") => new(label, value);

        public static DebugBoolArg Bool(string label, bool value = false) => new(label, value);

        public static DebugOptionsArg<T> Enum<T>(string label, T value = default) where T : struct, System.Enum
        {
            var values = (T[]) System.Enum.GetValues(typeof(T));
            return new DebugOptionsArg<T>(label, () => values, value, null);
        }

        // Fixed options, value is the initial choice.
        public static DebugOptionsArg<T> Options<T>(string label, IReadOnlyList<T> options, T value = default, Func<T, string> name = null) =>
            new(label, () => options, value, name);

        // Options read when shown, like a live player list.
        public static DebugOptionsArg<T> Options<T>(string label, Func<IReadOnlyList<T>> options, Func<T, string> name = null) =>
            new(label, options, default, name);
    }

    // Action argument. Keeps the last value for the session.
    public abstract class ADebugArg
    {
        public string Label { get; }

        // Raised when the value is changed from the UI
        internal event Action Changed;

        protected ADebugArg(string label)
        {
            Label = label ?? string.Empty;
        }

        // Editor row, built from the regular controls
        internal abstract ADebugControl CreateControl();

        protected void NotifyChanged() => Changed?.Invoke();
    }

    public abstract class ADebugArg<T> : ADebugArg
    {
        public T Value { get; set; }

        protected ADebugArg(string label, T value) : base(label)
        {
            Value = value;
        }

        protected void SetFromUI(T value)
        {
            Value = value;
            NotifyChanged();
        }
    }

    public sealed class DebugIntArg : ADebugArg<int>
    {
        private int? _min;
        private int? _max;
        private int _step = 1;
        private string _unit;
        private bool _slider;

        public DebugIntArg(string label, int value) : base(label, value) { }

        public DebugIntArg Range(int min, int max)
        {
            _min = min;
            _max = max;
            return this;
        }

        public DebugIntArg Step(int step)
        {
            _step = step;
            return this;
        }

        public DebugIntArg Unit(string unit)
        {
            _unit = unit;
            return this;
        }

        public DebugIntArg Slider()
        {
            _slider = true;
            return this;
        }

        internal override ADebugControl CreateControl()
        {
            var control = new DebugIntField(Label, () => Value, SetFromUI).Step(_step).Unit(_unit);
            if (_min.HasValue && _max.HasValue) control.Range(_min.Value, _max.Value);
            if (_slider) control.Slider();
            return control;
        }
    }

    public sealed class DebugFloatArg : ADebugArg<float>
    {
        private float? _min;
        private float? _max;
        private float _step;
        private string _unit;
        private string _format;

        public DebugFloatArg(string label, float value) : base(label, value) { }

        public DebugFloatArg Range(float min, float max)
        {
            _min = min;
            _max = max;
            return this;
        }

        public DebugFloatArg Step(float step)
        {
            _step = step;
            return this;
        }

        public DebugFloatArg Unit(string unit)
        {
            _unit = unit;
            return this;
        }

        public DebugFloatArg Format(string format)
        {
            _format = format;
            return this;
        }

        internal override ADebugControl CreateControl()
        {
            var control = new DebugFloatField(Label, () => Value, SetFromUI).Step(_step).Unit(_unit);
            if (_min.HasValue && _max.HasValue) control.Range(_min.Value, _max.Value);
            if (_format != null) control.Format(_format);
            return control;
        }
    }

    public sealed class DebugStringArg : ADebugArg<string>
    {
        public DebugStringArg(string label, string value) : base(label, value) { }

        internal override ADebugControl CreateControl() => new DebugTextField(Label, () => Value, SetFromUI);
    }

    public sealed class DebugBoolArg : ADebugArg<bool>
    {
        public DebugBoolArg(string label, bool value) : base(label, value) { }

        internal override ADebugControl CreateControl() => new DebugToggle(Label, () => Value, SetFromUI);
    }

    // Choice arguments, used by RunOnPick chips without knowing T.
    internal interface IDebugOptionsArg
    {
        IReadOnlyList<string> GetNames();
        int SelectedIndex { get; }
        void Select(int index);
    }

    public sealed class DebugOptionsArg<T> : ADebugArg<T>, IDebugOptionsArg
    {
        private readonly Func<IReadOnlyList<T>> _options;
        private readonly Func<T, string> _name;

        public DebugOptionsArg(string label, Func<IReadOnlyList<T>> options, T value, Func<T, string> name) : base(label, value)
        {
            _options = options;
            _name = name;
        }

        public IReadOnlyList<T> GetOptions() => _options?.Invoke() ?? Array.Empty<T>();

        public IReadOnlyList<string> GetNames() => GetOptions().Select(GetName).ToList();

        // Falls back to the first option when the value is not in the list
        public int SelectedIndex
        {
            get
            {
                var options = GetOptions();
                for (var i = 0; i < options.Count; i++)
                {
                    if (EqualityComparer<T>.Default.Equals(options[i], Value))
                        return i;
                }

                if (options.Count == 0)
                    return -1;
                Value = options[0];
                return 0;
            }
        }

        public void Select(int index)
        {
            var options = GetOptions();
            if (index >= 0 && index < options.Count)
                SetFromUI(options[index]);
        }

        private string GetName(T value) => _name != null ? _name(value) : value switch
        {
            null => "null",
            IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString()
        };

        internal override ADebugControl CreateControl() => new DebugDropdown(Label, () => SelectedIndex, Select, GetNames);
    }
}
