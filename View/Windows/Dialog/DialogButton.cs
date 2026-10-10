// Author: František Holubec
// Created: 09.10.2026

using System;
using EDIVE.VisualPresets.Switchers;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.View.Windows.Dialog
{
    [RequireComponent(typeof(Button))]
    public class DialogButton : MonoBehaviour
    {
        [SerializeField]
        private VisualSwitcher _Switcher = new();

        private Button _button;

        public DialogButtonSetup Setup { get; private set; }

        public event Action<DialogButton> Clicked;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClicked);
        }

        public void Apply(DialogButtonSetup setup)
        {
            Setup = setup;
            _Switcher.Apply(setup?.Visual);
        }

        private void OnClicked() => Clicked?.Invoke(this);
    }
}
