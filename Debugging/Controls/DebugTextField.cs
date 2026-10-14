// Author: František Holubec
// Created: 14.10.2026

using System;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    public sealed class DebugTextField : ADebugValueControl<DebugTextField, string>
    {
        public DebugTextField(string label, Func<string> getter, Action<string> setter) : base(label, getter, setter) { }

        public override DebugControlView CreateView() => new View(this);

        private sealed class View : DebugControlView
        {
            private readonly DebugTextField _control;
            private readonly TextField _field;

            public View(DebugTextField control) : base(control)
            {
                _control = control;
                AddLabel(control.Label);

                // Commits on enter or focus loss
                _field = new TextField {isDelayed = true, isReadOnly = control.IsReadOnly};
                _field.AddToClassList("dm-field");
                _field.AddToClassList("dm-field--text");
                _field.RegisterValueChangedCallback(evt => _control.SetValue(evt.newValue));
                Add(_field);
            }

            protected override void Pull() => _field.SetValueWithoutNotify(_control.GetValue() ?? string.Empty);
        }
    }
}
