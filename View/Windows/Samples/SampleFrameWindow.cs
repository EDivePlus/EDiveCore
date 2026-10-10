// Author: František Holubec
// Created: 09.10.2026

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using EDIVE.View.Toasts;
using EDIVE.View.ViewTree;
using EDIVE.View.Windows.Dialog;
using EDIVE.VisualPresets.Presets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.View.Windows.Samples
{
    // Frame with buttons for the other samples, context is its title
    public class SampleFrameWindow : AWindow<string, object>
    {
        [SerializeField]
        private TMP_Text _Title;

        [SerializeField]
        private TMP_Text _Status;

        [SerializeField]
        private Button _OpenFrameButton;

        [SerializeField]
        private Button _DialogButton;

        [SerializeField]
        private Button _ToastButton;

        [SerializeField]
        private Button _CloseButton;

        [SerializeField]
        private WindowDefinition _FrameWindow;

        [SerializeField]
        private WindowDefinition _DialogWindow;

        // Cycled by the dialog button
        [SerializeField]
        private List<DialogSetup> _DialogSetups = new();

        [SerializeField]
        private VisualPreset _ToastPreset = new();

        private int _dialogIndex;

        protected override string DefaultContext => "Frame";

        protected override void Awake()
        {
            base.Awake();
            _OpenFrameButton.onClick.AddListener(OnOpenFrameClicked);
            _DialogButton.onClick.AddListener(OnDialogClicked);
            _ToastButton.onClick.AddListener(OnToastClicked);
            _CloseButton.onClick.AddListener(OnCloseClicked);
        }

        protected override void OnContextSet(string context)
        {
            _Title.text = context;
            _Status.text = "";
        }

        protected override void OnWindowReady() => Debug.Log($"[Windows Sample] {_Title.text} ready");

        private void OnOpenFrameClicked() => Screen.Open(_FrameWindow, $"Frame {Screen.Handles.Count + 1}");

        private void OnDialogClicked() => DialogAsync().Forget();

        private async UniTaskVoid DialogAsync()
        {
            if (_DialogSetups.Count == 0)
                return;

            var setup = _DialogSetups[_dialogIndex++ % _DialogSetups.Count];
            var result = await Screen.Open(_DialogWindow, setup).Closed;
            if (this != null)
                _Status.text = $"{setup.name} result: {result ?? "null"}";
        }

        // Same preset again restarts the shown toast
        private void OnToastClicked() => this.FindInViewTree<ToastManager>()?.Show(_ToastPreset);

        private void OnCloseClicked() => Close();
    }
}
