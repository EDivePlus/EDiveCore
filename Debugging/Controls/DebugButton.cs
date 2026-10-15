// Author: František Holubec
// Created: 14.10.2026

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    public sealed class DebugButton : ADebugControl<DebugButton>
    {
        private readonly Action _action;

        internal bool IsPrimary { get; private set; }
        internal bool IsDanger { get; private set; }
        internal string ConfirmText { get; private set; }

        public DebugButton(string label, Action action) : base(label)
        {
            _action = action;
        }

        public DebugButton Primary()
        {
            IsPrimary = true;
            return this;
        }

        public DebugButton Danger()
        {
            IsDanger = true;
            return this;
        }

        // First click arms the button, second click within 3 s runs it.
        public DebugButton Confirm(string text = "Confirm?")
        {
            ConfirmText = text;
            return this;
        }

        public void Invoke()
        {
            try
            {
                _action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                DebugMenu.Toast($"{Label} failed: {e.GetType().Name}", DebugToastLevel.Error);
            }
        }

        public override DebugControlView CreateView() => new DebugButtonsView(this, new[] {this});
    }

    // Several buttons in one wrapping row.
    public sealed class DebugButtonRow : ADebugControl<DebugButtonRow>
    {
        private readonly List<DebugButton> _buttons = new();

        public IReadOnlyList<DebugButton> Buttons => _buttons;

        internal override string SearchText => string.Join(" ", _buttons.Select(b => b.Label));

        public DebugButtonRow(IEnumerable<(string label, Action action)> buttons) : base(string.Empty)
        {
            foreach (var (label, action) in buttons)
            {
                _buttons.Add(new DebugButton(label, action));
            }
        }

        // Styles one button of the row: row.Button(0).Primary().
        public DebugButton Button(int index) => _buttons[index];

        public override DebugControlView CreateView() => new DebugButtonsView(this, _buttons);
    }

    internal sealed class DebugButtonsView : DebugControlView
    {
        private const long CONFIRM_TIMEOUT_MS = 3000;

        public DebugButtonsView(ADebugControl control, IReadOnlyList<DebugButton> buttons) : base(control)
        {
            AddToClassList("dm-row--buttons");
            foreach (var button in buttons)
            {
                Add(CreateButton(button));
            }
        }

        protected override void Pull() { }

        private static Button CreateButton(DebugButton data)
        {
            var button = new Button {text = data.Label};
            button.AddToClassList("dm-button");
            button.EnableInClassList("dm-button--primary", data.IsPrimary);
            button.EnableInClassList("dm-button--danger", data.IsDanger);

            var armed = false;
            IVisualElementScheduledItem disarm = null;

            void Disarm()
            {
                armed = false;
                disarm?.Pause();
                button.text = data.Label;
                button.RemoveFromClassList("dm-button--armed");
            }

            button.clicked += () =>
            {
                if (data.ConfirmText != null && !armed)
                {
                    armed = true;
                    button.text = data.ConfirmText;
                    button.AddToClassList("dm-button--armed");
                    disarm = button.schedule.Execute(Disarm).StartingIn(CONFIRM_TIMEOUT_MS);
                    return;
                }

                Disarm();
                data.Invoke();
            };
            return button;
        }
    }
}
