// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Choice by index. Options are read on show and refresh, so they can change.
    public sealed class DebugDropdown : ADebugValueControl<DebugDropdown, int>
    {
        private Func<IReadOnlyList<string>> _options;

        public DebugDropdown(string label, Func<int> getter, Action<int> setter, Func<IReadOnlyList<string>> options) : base(label, getter, setter)
        {
            _options = options;
        }

        public DebugDropdown Options(IReadOnlyList<string> options)
        {
            _options = () => options;
            return this;
        }

        public DebugDropdown Options(Func<IReadOnlyList<string>> options)
        {
            _options = options;
            return this;
        }

        internal IReadOnlyList<string> GetOptions() => _options?.Invoke() ?? Array.Empty<string>();

        public override DebugControlView CreateView() => new View(this);

        private sealed class View : DebugControlView
        {
            private readonly DebugDropdown _control;
            private readonly DropdownField _field;

            public View(DebugDropdown control) : base(control)
            {
                _control = control;
                AddLabel(control.Label);

                _field = new DropdownField();
                _field.AddToClassList("dm-field");
                _field.AddToClassList("dm-dropdown");
                _field.SetEnabled(!control.IsReadOnly);
                _field.RegisterValueChangedCallback(_ => _control.SetValue(_field.index));
                Add(_field);
            }

            protected override void Pull()
            {
                var options = _control.GetOptions();
                if (!_field.choices.SequenceEqual(options))
                    _field.choices = options.ToList();

                var index = _control.GetValue();
                if (index >= 0 && index < options.Count)
                    _field.SetValueWithoutNotify(options[index]);
                else
                    _field.SetValueWithoutNotify(string.Empty);
            }
        }
    }
}
