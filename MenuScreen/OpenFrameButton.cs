// Author: Michal Petr
// Created: 05.03.2026

using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.MenuScreen
{
    [System.Obsolete("Replaced by the window system, see EDIVE.View.Windows (AppWindow, WindowDefinition, WindowCatalog)")]
    public class OpenFrameButton : MonoBehaviour
    {
        [SerializeField]
        [Required]
        private Button _OpenButton;
        
        [SerializeField]
        [Required]
        private MenuScreenFrame _FrameToOpen;
        
        private void Awake()
        {
            _OpenButton.onClick.AddListener(OnOpenButtonClicked);
        }
        
        private void OnDestroy()
        {
            if (_OpenButton)
                _OpenButton.onClick.RemoveListener(OnOpenButtonClicked);
        }
        
        private void OnOpenButtonClicked()
        {
            if (_FrameToOpen == null || _FrameToOpen.Controller == null)
                return;
            
            _FrameToOpen.Controller.OpenFrame(_FrameToOpen);
        }
    }
}
