// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Read-only value.
    public sealed class DebugWatch : ADebugControl<DebugWatch>
    {
        private readonly Func<object> _getter;
        private string _format;
        private string _unit;

        public DebugWatch(string label, Func<object> getter) : base(label)
        {
            _getter = getter;
        }

        // Format for numbers and other IFormattable values: "0.0", "N0", "HH:mm:ss".
        public DebugWatch Format(string format)
        {
            _format = format;
            return this;
        }

        public DebugWatch Unit(string unit)
        {
            _unit = unit;
            return this;
        }

        public string GetText()
        {
            var value = _getter?.Invoke();
            var text = value switch
            {
                null => "null",
                IFormattable formattable when _format != null => formattable.ToString(_format, CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
            return string.IsNullOrEmpty(_unit) ? text : $"{text} {_unit}";
        }

        public override DebugControlView CreateView() => new View(this);

        private sealed class View : DebugControlView
        {
            private readonly DebugWatch _control;
            private readonly Label _value;

            public View(DebugWatch control) : base(control)
            {
                _control = control;
                if (!string.IsNullOrEmpty(control.Label))
                    AddLabel(control.Label);

                _value = new Label();
                _value.AddToClassList("dm-value");
                _value.AddToClassList("dm-value--watch");
                _value.selection.isSelectable = true;
                Add(_value);
            }

            protected override void Pull() => _value.text = _control.GetText();
        }
    }
}
