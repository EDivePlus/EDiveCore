// Author: František Holubec
// Created: 14.10.2026

using System;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    public sealed class DebugToggle : ADebugValueControl<DebugToggle, bool>
    {
        public DebugToggle(string label, Func<bool> getter, Action<bool> setter) : base(label, getter, setter) { }

        public override DebugControlView CreateView() => new View(this);

        private sealed class View : DebugControlView
        {
            private readonly DebugToggle _control;
            private readonly DebugSwitch _switch;

            public View(DebugToggle control) : base(control)
            {
                _control = control;
                AddLabel(control.Label);
                _switch = new DebugSwitch();
                _switch.EnableInClassList("dm-switch--readonly", control.IsReadOnly);
                Add(_switch);

                // Whole row toggles, easier to hit by touch and in XR
                if (!control.IsReadOnly)
                {
                    AddToClassList("dm-row--clickable");
                    this.AddManipulator(new Clickable(() => _control.SetValue(!_control.GetValue())));
                }
            }

            protected override void Pull() => _switch.SetValue(_control.GetValue());
        }
    }

    // Switch track with a knob, input is handled by the owner.
    public sealed class DebugSwitch : VisualElement
    {
        public bool Value { get; private set; }

        public DebugSwitch()
        {
            AddToClassList("dm-switch");
            pickingMode = PickingMode.Ignore;
            var knob = new VisualElement {pickingMode = PickingMode.Ignore};
            knob.AddToClassList("dm-switch__knob");
            Add(knob);
        }

        public void SetValue(bool value)
        {
            Value = value;
            EnableInClassList("dm-switch--on", value);
        }
    }
}
