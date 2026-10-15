// Author: František Holubec
// Created: 15.10.2026

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace EDIVE.UIElements.Selectables
{
    // Minus and plus buttons around a whole number input
    public class InputStepper : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private TMP_InputField _Input;

        [SerializeField]
        [Required]
        private Button _DecrementButton;

        [SerializeField]
        [Required]
        private Button _IncrementButton;

        [SerializeField]
        private int _Min = 1;

        [SerializeField]
        private int _Max = 99;

        [SerializeField]
        [Min(1)]
        private int _Step = 1;

        public int Value => int.TryParse(_Input.text, out var value) ? value : _Min;

        private void OnEnable()
        {
            _DecrementButton.onClick.AddListener(Decrement);
            _IncrementButton.onClick.AddListener(Increment);
            _Input.onValueChanged.AddListener(OnInputChanged);
            RefreshButtons();
        }

        private void OnDisable()
        {
            _DecrementButton.onClick.RemoveListener(Decrement);
            _IncrementButton.onClick.RemoveListener(Increment);
            _Input.onValueChanged.RemoveListener(OnInputChanged);
        }

        public void Decrement() => SetValue(Value - _Step);
        public void Increment() => SetValue(Value + _Step);

        public void SetValue(int value)
        {
            _Input.text = Mathf.Clamp(value, _Min, _Max).ToString();
            RefreshButtons();
        }

        private void OnInputChanged(string text) => RefreshButtons();

        private void RefreshButtons()
        {
            var value = Value;
            _DecrementButton.interactable = value > _Min;
            _IncrementButton.interactable = value < _Max;
        }
    }
}
