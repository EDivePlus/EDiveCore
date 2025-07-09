// Author: František Holubec
// Created: 09.07.2025

using EDIVE.Core;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.XRTools.Tablet
{
    [RequireComponent(typeof(Button))]
    public class TabletToggleButton : MonoBehaviour
    {
        private Button _button;

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

        private void OnButtonClicked()
        {
            if (AppCore.Services.TryGet<TabletController>(out var tablet))
            {
                tablet.ToggleTablet();
            }
        }
    }
}
