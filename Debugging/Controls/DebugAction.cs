// Author: František Holubec
// Created: 15.10.2026

using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace EDIVE.Debugging
{
    // Method with arguments edited inline and a Run button. Funcs show their result.
    public sealed class DebugAction : ADebugControl<DebugAction>
    {
        private readonly Func<object> _invoke;
        private bool _closeOnRun;
        private string _openAfterRun;

        internal IReadOnlyList<ADebugArg> Args { get; }
        internal bool HasResult { get; }
        internal string RunText { get; private set; } = "Run";
        internal bool RunsOnPick { get; private set; }
        internal string LastResult { get; private set; }

        public DebugAction(string label, bool hasResult, Func<object> invoke, params ADebugArg[] args) : base(label)
        {
            HasResult = hasResult;
            _invoke = invoke;
            Args = args;
        }

        public DebugAction RunLabel(string text)
        {
            RunText = text;
            return this;
        }

        // Runs whenever an argument changes, no Run button. One option argument shows as chips.
        public DebugAction RunOnPick()
        {
            RunsOnPick = true;
            return this;
        }

        public DebugAction CloseOnRun()
        {
            _closeOnRun = true;
            return this;
        }

        // Page path or tool name opened after running.
        public DebugAction OpenAfterRun(string location)
        {
            _openAfterRun = location;
            return this;
        }

        public void Run()
        {
            try
            {
                var result = _invoke();
                if (HasResult)
                {
                    LastResult = FormatResult(result);
                    DebugMenu.Toast($"{Label}: {LastResult}", DebugToastLevel.Success);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (HasResult)
                    LastResult = e.GetType().Name;
                DebugMenu.Toast($"{Label} failed: {e.GetType().Name}", DebugToastLevel.Error);
            }

            Refresh();

            if (_closeOnRun) DebugMenu.Close();
            else if (_openAfterRun != null) DebugMenu.Open(_openAfterRun);
        }

        private static string FormatResult(object result) => result switch
        {
            null => "null",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => result.ToString()
        };

        public override DebugControlView CreateView() => new DebugActionView(this);
    }

    internal sealed class DebugActionView : DebugControlView
    {
        private readonly DebugAction _action;
        private readonly VisualElement _chips;
        private readonly IDebugOptionsArg _chipArg;
        private readonly VisualElement _resultRow;
        private readonly Label _result;
        private IReadOnlyList<string> _chipNames;

        public DebugActionView(DebugAction action) : base(action)
        {
            _action = action;
            AddToClassList("dm-row--action");

            var header = new VisualElement();
            header.AddToClassList("dm-action__header");
            header.Add(DebugIcons.Create(action.RunsOnPick ? "bolt" : "function", "dm-action__icon"));
            var title = new Label(action.Label);
            title.AddToClassList("dm-action__title");
            header.Add(title);
            Add(header);

            if (action.RunsOnPick && action.Args.Count == 1 && action.Args[0] is IDebugOptionsArg optionsArg)
            {
                _chipArg = optionsArg;
                _chips = new VisualElement();
                _chips.AddToClassList("dm-action__chips");
                Add(_chips);
            }
            else
            {
                foreach (var arg in action.Args)
                {
                    var argView = arg.CreateControl().CreateView();
                    argView.AddToClassList("dm-action__arg");
                    Add(argView);
                }

                if (!action.RunsOnPick)
                {
                    var run = new Button(action.Run);
                    run.AddToClassList("dm-button");
                    run.AddToClassList("dm-button--primary");
                    run.AddToClassList("dm-action__run");
                    run.Add(DebugIcons.Create("play_arrow", "dm-action__run-icon"));
                    run.Add(new Label(action.RunText));
                    Add(run);
                }
            }

            _resultRow = new VisualElement();
            _resultRow.AddToClassList("dm-action__result-row");
            var resultLabel = new Label("Result");
            resultLabel.AddToClassList(LABEL_CLASS);
            _resultRow.Add(resultLabel);
            _result = new Label();
            _result.AddToClassList("dm-value");
            _result.AddToClassList("dm-action__result");
            _result.selection.isSelectable = true;
            _resultRow.Add(_result);
            Add(_resultRow);

            if (action.RunsOnPick && _chips == null)
            {
                RegisterCallback<AttachToPanelEvent>(_ => SubscribeArgs(true));
                RegisterCallback<DetachFromPanelEvent>(_ => SubscribeArgs(false));
            }
        }

        protected override void Pull()
        {
            var hasResult = _action.HasResult && _action.LastResult != null;
            _resultRow.style.display = hasResult ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasResult)
                _result.text = _action.LastResult;

            if (_chips != null)
                UpdateChips();
        }

        private void UpdateChips()
        {
            var names = _chipArg.GetNames();
            if (_chipNames == null || !EqualNames(_chipNames, names))
            {
                _chipNames = names;
                _chips.Clear();
                for (var i = 0; i < names.Count; i++)
                {
                    var index = i;
                    var chip = new Button(() =>
                    {
                        _chipArg.Select(index);
                        _action.Run();
                    }) {text = names[i]};
                    chip.AddToClassList("dm-option-chip");
                    _chips.Add(chip);
                }
            }

            var selected = _chipArg.SelectedIndex;
            for (var i = 0; i < _chips.childCount; i++)
            {
                _chips[i].EnableInClassList("dm-option-chip--selected", i == selected);
            }
        }

        private void SubscribeArgs(bool subscribe)
        {
            foreach (var arg in _action.Args)
            {
                if (subscribe) arg.Changed += _action.Run;
                else arg.Changed -= _action.Run;
            }
        }

        private static bool EqualNames(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            if (a.Count != b.Count)
                return false;
            for (var i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }
    }
}
