// Author: Michal Petr
// Created: 01.06.2026

using EDIVE.Core;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.MenuScreen
{
    public class CloseWidgetButton : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private Button _CloseButton;
        
        private void OnEnable()
        {
            if (_CloseButton)
                _CloseButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnDisable()
        {
            if (_CloseButton)
                _CloseButton.onClick.RemoveListener(OnCloseButtonClicked);
        }
        
        private void OnCloseButtonClicked()
        {
            AppCore.Services.TryGet(out MenuScreenController controller);
            if (controller == null)
            {
                Debug.LogError("MenuScreenController not found in scene. CloseWidgetButton requires MenuScreenController to function.");
                return;
            }
            
            controller.CollapseCurrentFrame();
        }
    }
}
