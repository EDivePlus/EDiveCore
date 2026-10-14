// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Int as a stepper, or a slider with .Slider().
    public sealed class DebugIntField : ADebugValueControl<DebugIntField, int>
    {
        internal int Min { get; private set; } = int.MinValue;
        internal int Max { get; private set; } = int.MaxValue;
        internal int StepSize { get; private set; } = 1;
        internal string UnitText { get; private set; }
        internal bool UseSlider { get; private set; }
        internal bool HasRange => Min != int.MinValue && Max != int.MaxValue;

        public DebugIntField(string label, Func<int> getter, Action<int> setter) : base(label, getter, setter) { }

        public DebugIntField Range(int min, int max)
        {
            Min = Mathf.Min(min, max);
            Max = Mathf.Max(min, max);
            return this;
        }

        public DebugIntField Step(int step)
        {
            StepSize = Mathf.Max(1, step);
            return this;
        }

        public DebugIntField Unit(string unit)
        {
            UnitText = unit;
            return this;
        }

        // Slider instead of the stepper, needs Range.
        public DebugIntField Slider()
        {
            UseSlider = true;
            return this;
        }

        internal void SetClamped(int value) => SetValue(Mathf.Clamp(value, Min, Max));

        internal string FormatValue(int value) => string.IsNullOrEmpty(UnitText) ? value.ToString(CultureInfo.InvariantCulture) : $"{value} {UnitText}";

        public override DebugControlView CreateView()
        {
            if (UseSlider && HasRange)
                return new DebugSliderView(this, Min, Max, true, () => GetValue(), v => SetClamped(Mathf.RoundToInt(v)), v => FormatValue(Mathf.RoundToInt(v)), IsReadOnly);
            return new StepperView(this);
        }

        private sealed class StepperView : DebugControlView
        {
            private readonly DebugIntField _control;
            private readonly IntegerField _field;
            private readonly Label _value;

            public StepperView(DebugIntField control) : base(control)
            {
                _control = control;
                AddLabel(control.Label);

                if (control.IsReadOnly)
                {
                    _value = new Label();
                    _value.AddToClassList("dm-value");
                    Add(_value);
                    return;
                }

                var stepper = new VisualElement();
                stepper.AddToClassList("dm-stepper");
                stepper.Add(CreateStepButton("-", -1));

                _field = new IntegerField {isDelayed = true};
                _field.AddToClassList("dm-stepper__field");
                _field.RegisterValueChangedCallback(evt => _control.SetClamped(evt.newValue));
                stepper.Add(_field);

                stepper.Add(CreateStepButton("+", 1));
                Add(stepper);

                if (!string.IsNullOrEmpty(control.UnitText))
                {
                    var unit = new Label(control.UnitText);
                    unit.AddToClassList("dm-unit");
                    Add(unit);
                }
            }

            private Button CreateStepButton(string text, int direction)
            {
                var button = new Button(() => _control.SetClamped(_control.GetValue() + direction * _control.StepSize)) {text = text};
                button.AddToClassList("dm-stepper__button");
                return button;
            }

            protected override void Pull()
            {
                var value = _control.GetValue();
                if (_field != null) _field.SetValueWithoutNotify(value);
                if (_value != null) _value.text = _control.FormatValue(value);
            }
        }
    }

    // Float as a field, or a slider when it has a Range.
    public sealed class DebugFloatField : ADebugValueControl<DebugFloatField, float>
    {
        internal float Min { get; private set; } = float.NegativeInfinity;
        internal float Max { get; private set; } = float.PositiveInfinity;
        internal float StepSize { get; private set; }
        internal string UnitText { get; private set; }
        internal string FormatText { get; private set; } = "0.##";
        internal bool HasRange => !float.IsInfinity(Min) && !float.IsInfinity(Max);

        public DebugFloatField(string label, Func<float> getter, Action<float> setter) : base(label, getter, setter) { }

        public DebugFloatField Range(float min, float max)
        {
            Min = Mathf.Min(min, max);
            Max = Mathf.Max(min, max);
            return this;
        }

        // Snaps set values to multiples of step.
        public DebugFloatField Step(float step)
        {
            StepSize = Mathf.Max(0f, step);
            return this;
        }

        public DebugFloatField Unit(string unit)
        {
            UnitText = unit;
            return this;
        }

        // Number format of the shown value, "0.##" by default.
        public DebugFloatField Format(string format)
        {
            FormatText = format;
            return this;
        }

        internal void SetSnapped(float value)
        {
            if (StepSize > 0f)
                value = Mathf.Round(value / StepSize) * StepSize;
            SetValue(Mathf.Clamp(value, Min, Max));
        }

        internal string FormatValue(float value)
        {
            var text = value.ToString(FormatText, CultureInfo.InvariantCulture);
            return string.IsNullOrEmpty(UnitText) ? text : $"{text} {UnitText}";
        }

        public override DebugControlView CreateView()
        {
            if (HasRange)
                return new DebugSliderView(this, Min, Max, false, GetValue, SetSnapped, FormatValue, IsReadOnly);
            return new FieldView(this);
        }

        private sealed class FieldView : DebugControlView
        {
            private readonly DebugFloatField _control;
            private readonly FloatField _field;
            private readonly Label _value;

            public FieldView(DebugFloatField control) : base(control)
            {
                _control = control;
                AddLabel(control.Label);

                if (control.IsReadOnly)
                {
                    _value = new Label();
                    _value.AddToClassList("dm-value");
                    Add(_value);
                    return;
                }

                _field = new FloatField {isDelayed = true};
                _field.AddToClassList("dm-field");
                _field.RegisterValueChangedCallback(evt => _control.SetSnapped(evt.newValue));
                Add(_field);

                if (!string.IsNullOrEmpty(control.UnitText))
                {
                    var unit = new Label(control.UnitText);
                    unit.AddToClassList("dm-unit");
                    Add(unit);
                }
            }

            protected override void Pull()
            {
                var value = _control.GetValue();
                if (_field != null) _field.SetValueWithoutNotify(value);
                if (_value != null) _value.text = _control.FormatValue(value);
            }
        }
    }

    // Label and value on top, slider below.
    internal sealed class DebugSliderView : DebugControlView
    {
        private readonly Func<float> _getter;
        private readonly Func<float, string> _format;
        private readonly Slider _slider;
        private readonly Label _value;

        public DebugSliderView(ADebugControl control, float min, float max, bool whole, Func<float> getter, Action<float> setter, Func<float, string> format, bool readOnly) : base(control)
        {
            _getter = getter;
            _format = format;
            AddToClassList("dm-row--slider");

            var header = new VisualElement();
            header.AddToClassList("dm-slider__header");
            var label = new Label(control.Label);
            label.AddToClassList(LABEL_CLASS);
            header.Add(label);
            _value = new Label();
            _value.AddToClassList("dm-value");
            header.Add(_value);
            Add(header);

            _slider = new Slider(min, max);
            _slider.AddToClassList("dm-slider");
            _slider.SetEnabled(!readOnly);
            _slider.RegisterValueChangedCallback(evt =>
            {
                var value = whole ? Mathf.Round(evt.newValue) : evt.newValue;
                _value.text = _format(value);
                setter(value);
            });
            Add(_slider);
        }

        protected override void Pull()
        {
            var value = _getter();
            _slider.SetValueWithoutNotify(value);
            _value.text = _format(value);
        }
    }
}
