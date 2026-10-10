// Author: František Holubec
// Created: 09.10.2026

using Cysharp.Threading.Tasks;
using EDIVE.Utils.Actions;
using EDIVE.View.ViewTree;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.View.Windows.Dialog
{
    // Opens a dialog in the nearest screen, runs the result if it is an action
    [RequireComponent(typeof(Button))]
    public class DialogOpenButton : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private WindowDefinition _DialogWindow;

        [SerializeField]
        [Required]
        private DialogSetup _Setup;

        private Button _button;
        private bool _isOpen;

        private void Awake()
        {
            if (TryGetComponent(out _button))
                _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnButtonClicked);
        }

        private void OnButtonClicked() => OpenAsync().Forget();

        private async UniTaskVoid OpenAsync()
        {
            if (_isOpen)
                return;

            var screen = this.FindInViewTree<ScreenRoot>();
            if (screen == null)
            {
                Debug.LogWarning($"[DialogOpenButton] {name} is not under a ScreenRoot.", this);
                return;
            }

            _isOpen = true;
            var result = await screen.Open(_DialogWindow, _Setup).Closed;
            _isOpen = false;

            if (result is IAction action)
                await action.Execute();
        }
    }
}
