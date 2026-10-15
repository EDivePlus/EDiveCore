// Author: František Holubec
// Created: 15.10.2026

using EDIVE.StateHandling.ToggleStates;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace EDIVE.UIElements.Selectables
{
    // Shows or hides a secret input, hidden by default
    public class InputVisibilityToggle : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private TMP_InputField _Input;

        [SerializeField]
        [Required]
        private Button _Button;

        // On while the text is visible, e.g. eye icon
        [SerializeField]
        private AToggleState _VisibleState;

        public bool IsVisible { get; private set; }

        private void OnEnable()
        {
            _Button.onClick.AddListener(Toggle);
            SetVisible(false);
        }

        private void OnDisable()
        {
            _Button.onClick.RemoveListener(Toggle);
        }

        public void Toggle() => SetVisible(!IsVisible);

        public void SetVisible(bool visible)
        {
            IsVisible = visible;
            _Input.contentType = visible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
            _Input.ForceLabelUpdate();
            if (_VisibleState != null)
                _VisibleState.SetState(visible);
        }
    }
}
