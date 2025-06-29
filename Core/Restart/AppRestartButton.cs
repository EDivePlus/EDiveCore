// Author: František Holubec
// Created: 29.06.2025

using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.Core.Restart
{
    [RequireComponent(typeof(Button))]
    public class AppRestartButton : MonoBehaviour
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
            AppRestartUtility.Restart();
        }
    }
}
