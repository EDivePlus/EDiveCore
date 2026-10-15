// Author: Michal Petr
// Created: 03.03.2026

using System;
using EDIVE.StateHandling.ToggleStates;
using EDIVE.View.Tooltips;
using EDIVE.VisualPresets.Switchers;
using UnityEngine;
using UnityEngine.UI;

namespace EDIVE.MenuScreen
{
    [System.Obsolete("Replaced by the window system, see EDIVE.View.Windows (AppWindow, WindowDefinition, WindowCatalog)")]
    public class WidgetDefinitionDisplay : MonoBehaviour
    {
        [SerializeField]
        private VisualSwitcher _Switcher;

        [SerializeField]
        private Button _OpenButton;
        
        [SerializeField]
        private TooltipTrigger _TooltipTrigger;

        [Tooltip("On while the widget has a live frame")]
        [SerializeField]
        private AToggleState _RunningState;
        
        public WidgetDefinition Definition { get; private set; }
        public event Action<WidgetDefinition> OnClick = delegate { };

        private void Awake()
        {
            _OpenButton.onClick.AddListener(OnOpenButtonClicked);
        }

        private void OnDestroy()
        {
            if (_OpenButton != null)
                _OpenButton.onClick.RemoveListener(OnOpenButtonClicked);
        }

        public void SetDefinition(WidgetDefinition definition)
        {
            if (definition == null)
                return;
            
            Definition = definition;
            if (_Switcher != null)
                _Switcher.Apply(definition.Visual);
            if (_TooltipTrigger)
                _TooltipTrigger.SetPreset(definition.Visual);
        }

        public void SetRunning(bool running)
        {
            if (_RunningState)
                _RunningState.State = running;
        }

        private void OnOpenButtonClicked()
        {
            OnClick?.Invoke(Definition);
        }
    }
}
