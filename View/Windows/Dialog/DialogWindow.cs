// Author: František Holubec
// Created: 09.10.2026

using System.Collections.Generic;
using EDIVE.OdinExtensions.Attributes;
using EDIVE.VisualPresets.Switchers;
using UnityEngine;

namespace EDIVE.View.Windows.Dialog
{
    // Content and buttons from an IDialogSetup, closes with the pressed button result
    public class DialogWindow : AWindow<IDialogSetup, object>
    {
        [SerializeField]
        [EnhancedBoxGroup("Dialog")]
        private VisualSwitcher _Switcher = new();

        // Filled in order, unused ones get disabled
        [SerializeField]
        [EnhancedBoxGroup("Dialog")]
        private List<DialogButton> _Buttons = new();

        protected override void Awake()
        {
            base.Awake();
            foreach (var button in _Buttons)
            {
                if (button != null)
                    button.Clicked += OnButtonClicked;
            }
        }

        protected override void OnDestroy()
        {
            foreach (var button in _Buttons)
            {
                if (button != null)
                    button.Clicked -= OnButtonClicked;
            }
            base.OnDestroy();
        }

        protected override void OnContextSet(IDialogSetup setup)
        {
            if (setup == null)
                Debug.LogError($"[DialogWindow] {name} opened without a dialog setup.", this);

            _Switcher.Apply(setup?.Visual);

            var setups = setup?.Buttons;
            var count = setups?.Count ?? 0;
            if (count > _Buttons.Count)
                Debug.LogWarning($"[DialogWindow] {name} has {_Buttons.Count} buttons, setup needs {count}.", this);

            for (var i = 0; i < _Buttons.Count; i++)
            {
                var button = _Buttons[i];
                if (button == null)
                    continue;

                var used = i < count;
                button.gameObject.SetActive(used);
                button.Apply(used ? setups[i] : null);
            }
        }

        private void OnButtonClicked(DialogButton button) => Close(button.Setup?.ResultValue);
    }
}
